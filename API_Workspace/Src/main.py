import base64
import os
from datetime import datetime, timezone
from functools import lru_cache
from typing import Optional

from fastapi import Depends, FastAPI, Header, HTTPException, Query, status
from pydantic import BaseModel, Field
from sqlalchemy import (
    DateTime,
    Float,
    ForeignKey,
    Integer,
    String,
    create_engine,
    delete,
    func,
    select,
)
from sqlalchemy.orm import DeclarativeBase, Mapped, Session, mapped_column, sessionmaker

from cryptography.hazmat.primitives import serialization
from cryptography.hazmat.primitives.asymmetric import padding


DATABASE_URL = os.getenv(
    "DATABASE_URL",
    "postgresql+psycopg2://safechem:safechem@localhost:5432/safechem",
)
PRIVATE_KEY_PATH = os.getenv("PRIVATE_KEY_PATH", "./keys/private_key.pem")
GAME_APP_ID = os.getenv("GAME_APP_ID", "safechem-unity-client")
MAX_SKEW_SECONDS = 300


def utc_now() -> datetime:
    return datetime.now(timezone.utc)


class Base(DeclarativeBase):
    pass


class Player(Base):
    __tablename__ = "players"

    player_uuid: Mapped[str] = mapped_column(String(64), primary_key=True)
    pseudo: Mapped[str] = mapped_column(String(120), nullable=False)
    created_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), nullable=False)
    updated_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), nullable=False)
    deleted_at: Mapped[Optional[datetime]] = mapped_column(DateTime(timezone=True), nullable=True)


class LevelResult(Base):
    __tablename__ = "level_results"

    id: Mapped[int] = mapped_column(Integer, primary_key=True, autoincrement=True)
    player_uuid: Mapped[str] = mapped_column(
        String(64), ForeignKey("players.player_uuid", ondelete="CASCADE"), nullable=False
    )
    level_index: Mapped[int] = mapped_column(Integer, nullable=False)
    level_id: Mapped[Optional[str]] = mapped_column(String(64), nullable=True)
    duration_seconds: Mapped[float] = mapped_column(Float, nullable=False)
    stars: Mapped[int] = mapped_column(Integer, nullable=False)
    sent_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), nullable=False)
    created_at: Mapped[datetime] = mapped_column(DateTime(timezone=True), nullable=False)


engine = create_engine(DATABASE_URL, pool_pre_ping=True, future=True)
SessionLocal = sessionmaker(bind=engine, autoflush=False, autocommit=False, future=True)

app = FastAPI(title="SafeChem API", version="1.0.0")


class CreatePlayerRequest(BaseModel):
    player_uuid: str = Field(min_length=3, max_length=64)
    pseudo: str = Field(min_length=1, max_length=120)
    sent_at: datetime


class UpdatePseudoRequest(BaseModel):
    pseudo: str = Field(min_length=1, max_length=120)
    sent_at: datetime


class LevelFinishedRequest(BaseModel):
    player_uuid: str = Field(min_length=3, max_length=64)
    level_index: int = Field(ge=0, le=9999)
    level_id: Optional[str] = Field(default=None, max_length=64)
    duration_seconds: float = Field(ge=0.0, le=999999.0)
    stars: int = Field(ge=0, le=3)
    sent_at: datetime


def get_db() -> Session:
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()


@lru_cache(maxsize=1)
def load_private_key():
    try:
        with open(PRIVATE_KEY_PATH, "rb") as handle:
            return serialization.load_pem_private_key(handle.read(), password=None)
    except FileNotFoundError:
        raise RuntimeError(f"Private key not found at {PRIVATE_KEY_PATH}")


def verify_game_proof(x_game_proof: str = Header(..., alias="X-Game-Proof")):
    try:
        cipher = base64.b64decode(x_game_proof)
        plain = load_private_key().decrypt(cipher, padding.PKCS1v15()).decode("utf-8")
        app_id, unix_ts_raw = plain.split("|", 1)
        unix_ts = int(unix_ts_raw)
        now_ts = int(utc_now().timestamp())
    except Exception as exception:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail=f"Invalid game proof: {exception}",
        )

    if app_id != GAME_APP_ID:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Unknown game app id.",
        )

    if abs(now_ts - unix_ts) > MAX_SKEW_SECONDS:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Expired request proof.",
        )


@app.on_event("startup")
def startup_event():
    Base.metadata.create_all(bind=engine)


@app.get("/health")
def health():
    return {"status": "ok", "at": utc_now()}


@app.post("/players", dependencies=[Depends(verify_game_proof)])
def create_player(payload: CreatePlayerRequest, db: Session = Depends(get_db)):
    existing = db.get(Player, payload.player_uuid)
    now = utc_now()
    if existing is None:
        player = Player(
            player_uuid=payload.player_uuid.strip(),
            pseudo=payload.pseudo.strip(),
            created_at=payload.sent_at,
            updated_at=now,
            deleted_at=None,
        )
        db.add(player)
        db.commit()
        return {"status": "created", "player_uuid": payload.player_uuid, "at": now}

    existing.pseudo = payload.pseudo.strip()
    existing.updated_at = now
    existing.deleted_at = None
    db.commit()
    return {"status": "updated", "player_uuid": payload.player_uuid, "at": now}


@app.patch("/players/{player_uuid}/pseudo", dependencies=[Depends(verify_game_proof)])
def update_pseudo(player_uuid: str, payload: UpdatePseudoRequest, db: Session = Depends(get_db)):
    player = db.get(Player, player_uuid)
    if player is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Player not found.")

    player.pseudo = payload.pseudo.strip()
    player.updated_at = payload.sent_at
    db.commit()
    return {"status": "updated", "player_uuid": player_uuid, "pseudo": player.pseudo}


@app.delete("/players/{player_uuid}", dependencies=[Depends(verify_game_proof)])
def delete_player(
    player_uuid: str,
    sent_at: Optional[datetime] = Query(default=None),
    db: Session = Depends(get_db),
):
    player = db.get(Player, player_uuid)
    if player is None:
        return {"status": "not_found", "player_uuid": player_uuid}

    db.execute(delete(LevelResult).where(LevelResult.player_uuid == player_uuid))
    db.delete(player)
    db.commit()
    return {"status": "deleted", "player_uuid": player_uuid, "at": sent_at or utc_now()}


@app.post("/levels/finished", dependencies=[Depends(verify_game_proof)])
def level_finished(payload: LevelFinishedRequest, db: Session = Depends(get_db)):
    player = db.get(Player, payload.player_uuid)
    if player is None:
        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Player not found.")

    result = LevelResult(
        player_uuid=payload.player_uuid,
        level_index=payload.level_index,
        level_id=payload.level_id.strip() if payload.level_id else None,
        duration_seconds=payload.duration_seconds,
        stars=payload.stars,
        sent_at=payload.sent_at,
        created_at=utc_now(),
    )
    db.add(result)

    player.updated_at = utc_now()
    db.commit()

    total_results = db.scalar(
        select(func.count(LevelResult.id)).where(LevelResult.player_uuid == payload.player_uuid)
    )
    return {
        "status": "saved",
        "player_uuid": payload.player_uuid,
        "level_index": payload.level_index,
        "stars": payload.stars,
        "results_count": total_results,
    }
