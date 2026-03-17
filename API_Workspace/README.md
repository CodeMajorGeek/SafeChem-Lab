# SafeChem REST API Workspace

## Stack
- FastAPI (Python)
- PostgreSQL
- Adminer
- Docker Compose

## Lancer l'API
```bash
cd API_Workspace
docker compose up --build
```

## Repartir avec une base vide (dev)
```bash
docker compose down -v
docker compose up --build
```

## URLs
- API: `http://localhost:8000`
- Swagger: `http://localhost:8000/docs`
- Adminer: `http://localhost:8080`

## DB (Adminer)
- System: `PostgreSQL`
- Server: `db`
- User: `safechem`
- Password: `safechem`
- Database: `safechem`

## Clés
- Privée API (serveur): `API_Workspace/Src/keys/private_key.pem`
- Publique Unity (Resources): `Assets/Resources/Security/game_public_key.xml`

## Régénérer la paire de clés
```bash
python API_Workspace/Src/tools/generate_keys.py
```
