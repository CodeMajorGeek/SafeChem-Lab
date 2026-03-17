# SafeChem API - Documentation

## 1) Objectif
Cette API REST Python sert a synchroniser des donnees du jeu Unity:
- creation de player
- suppression de player
- mise a jour du pseudo
- enregistrement d'un niveau termine (temps + etoiles)

Le backend est volontairement simple et lisible (pas de couche service/repository inutile pour ce scope).

---

## 2) Stack technique
- **API**: FastAPI (`main.py`)
- **ORM**: SQLAlchemy
- **DB**: PostgreSQL 16
- **UI DB**: Adminer
- **Infra locale**: Docker Compose

---

## 3) Stack Docker
Fichier: `API_Workspace/docker-compose.yml`

Services:
- `api`
  - build depuis `API_Workspace/Src/Dockerfile`
  - code monte en volume depuis `./Src` vers `/app`
  - port expose: `8000`
  - variables:
    - `DATABASE_URL`
    - `PRIVATE_KEY_PATH`
    - `GAME_APP_ID`
- `db`
  - image `postgres:16`
  - port expose: `5432`
  - volume persistant: `pgdata`
- `adminer`
  - image `adminer:latest`
  - port expose: `8080`

---

## 4) Demarrage
Depuis `API_Workspace`:

```bash
docker compose up --build
```

Acces:
- API: `http://localhost:8000`
- Swagger: `http://localhost:8000/docs`
- Adminer: `http://localhost:8080`

Reset DB dev:

```bash
docker compose down -v
docker compose up --build
```

---

## 5) Modele de donnees (MLD)
Diagramme PlantUML:
- `API_Workspace/db_mld.puml`

Tables:
- `players`
  - `player_uuid` (PK)
  - `pseudo`
  - `created_at`
  - `updated_at`
  - `deleted_at` (reserve)
- `level_results`
  - `id` (PK)
  - `player_uuid` (FK -> players.player_uuid)
  - `level_index`
  - `level_id` (optionnel)
  - `duration_seconds`
  - `stars` (0..3)
  - `sent_at`
  - `created_at`

Relation:
- `players` 1 -> N `level_results`

---

## 6) Securite des appels (cle publique/privee)

### Principe
Unity possede la **cle publique** (`Assets/Resources/Security/game_public_key.xml`).
Le serveur API possede la **cle privee** (`API_Workspace/Src/keys/private_key.pem`).

A chaque requete, Unity envoie le header:
- `X-Game-Proof`

Ce header contient un payload chiffre RSA (PKCS#1 v1.5):
- payload clair: `safechem-unity-client|<unix_timestamp>`

Le serveur:
1. dechiffre avec la cle privee
2. verifie `GAME_APP_ID`
3. verifie la fraicheur temporelle (skew max: 300s)

Si invalide: HTTP 401.

---

## 7) Endpoints REST

### `GET /health`
Healthcheck API.

### `POST /players`
Creer (ou upsert) un player.

Body:
```json
{
  "player_uuid": "uuid-ou-id",
  "pseudo": "Pseudo",
  "sent_at": "2026-03-17T12:00:00Z"
}
```

### `PATCH /players/{player_uuid}/pseudo`
Mettre a jour le pseudo.

Body:
```json
{
  "pseudo": "NouveauPseudo",
  "sent_at": "2026-03-17T12:00:00Z"
}
```

### `DELETE /players/{player_uuid}?sent_at=...`
Supprimer un player et ses resultats (`level_results`) en cascade logique API.

### `POST /levels/finished`
Enregistrer un niveau termine.

Body:
```json
{
  "player_uuid": "uuid-ou-id",
  "level_index": 2,
  "level_id": "level-2",
  "duration_seconds": 83.2,
  "stars": 3,
  "sent_at": "2026-03-17T12:00:00Z"
}
```

---

## 8) Arborescence utile
- `API_Workspace/Src/main.py`: API FastAPI + schema SQLAlchemy
- `API_Workspace/Src/requirements.txt`: dependances Python
- `API_Workspace/Src/Dockerfile`: image API
- `API_Workspace/Src/keys/private_key.pem`: cle privee serveur
- `API_Workspace/Src/tools/generate_keys.py`: generation paire de cles
- `Assets/Resources/Security/game_public_key.xml`: cle publique Unity

---

## 9) Note d'implementation Unity
Le client Unity est dans:
- `Assets/Scripts/BackendApiClient.cs`

Il envoie automatiquement:
- create player
- delete player
- update pseudo
- level finished

Toutes les requetes ajoutent `X-Game-Proof`.
