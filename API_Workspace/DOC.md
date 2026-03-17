# SafeChem API - Documentation Technique

Ce document décrit plus en détail le backend Python associé à `SafeChem Lab`.

Le `README.md` du dossier `API_Workspace` donne une vue rapide pour lancer le workspace et comprendre son rôle.  
Ici, l'objectif est plutôt de documenter la logique du backend, ses choix de structure, son modèle de données et la manière dont il s'insère dans l'ensemble du projet.

---

## 1. Intention du backend

Le jeu Unity fonctionne d'abord localement.  
Le backend n'a donc pas été conçu comme un coeur de gameplay distant, mais comme un service de synchronisation léger.

Il permet de conserver quelques informations utiles hors du client :

- l'identité technique du joueur ;
- son pseudo ;
- les résultats remontés lorsqu'un niveau est terminé.

Le choix a été de rester très simple :

- peu de routes ;
- peu de tables ;
- peu de couches d'abstraction ;
- du code lisible avant tout.

Dans le contexte d'un prototype de serious game, cette simplicité est volontaire. Elle rend le backend plus rapide à faire évoluer et plus facile à relire.

---

## 2. Place de l'API dans l'architecture globale

Dans l'architecture actuelle, Unity reste responsable de :

- l'interface ;
- la progression locale ;
- la logique de jeu ;
- le calcul du score ;
- la persistance locale du profil joueur.

Le backend intervient seulement lorsque le jeu veut synchroniser certains événements :

- création du profil joueur ;
- mise à jour du pseudo ;
- suppression du profil ;
- remontée d'un niveau terminé.

Autrement dit, l'API complète Unity, mais ne le pilote pas.

Cette décision a plusieurs avantages :

- le prototype reste jouable même si l'API n'est pas disponible ;
- les problèmes réseau ne bloquent pas la boucle de jeu ;
- le backend reste suffisamment petit pour être compris rapidement.

---

## 3. Stack technique retenue

Le backend repose sur une stack courte et classique :

- `FastAPI` pour exposer les routes HTTP ;
- `SQLAlchemy` pour la couche ORM ;
- `PostgreSQL 16` pour la base de données ;
- `Adminer` pour explorer les données en local ;
- `Docker Compose` pour lancer l'ensemble proprement.

Cette combinaison répond bien au besoin actuel :

- FastAPI apporte une API lisible, un typage agréable et une documentation Swagger automatique ;
- PostgreSQL reste une base solide et standard ;
- Docker Compose simplifie énormément la mise en route pour le développement.

---

## 4. Organisation du workspace

Structure utile :

```text
API_Workspace/
├─ docker-compose.yml
├─ README.md
├─ DOC.md
├─ db_mld.puml
└─ Src/
   ├─ Dockerfile
   ├─ main.py
   ├─ requirements.txt
   ├─ keys/
   │  └─ private_key.pem
   └─ tools/
      └─ generate_keys.py
```

### `docker-compose.yml`

Déclare les services `api`, `db` et `adminer`.

### `Src/main.py`

Fichier principal du backend.  
Il contient :

- la configuration FastAPI ;
- les schémas SQLAlchemy ;
- les modèles d'entrée ;
- la vérification de preuve RSA ;
- les routes REST.

Le fichier est volontairement centralisé. À ce stade du projet, ce choix reste plus lisible qu'une découpe artificielle en couches nombreuses.

### `Src/requirements.txt`

Liste les dépendances Python du service API.

### `Src/keys/private_key.pem`

Clé privée utilisée par le serveur pour vérifier la preuve envoyée par Unity.

### `Src/tools/generate_keys.py`

Petit script utilitaire pour régénérer la paire de clés RSA.

### `db_mld.puml`

Source PlantUML du modèle logique de données.

---

## 5. Exécution locale avec Docker

Le workspace a été pensé pour qu'un développeur puisse l'utiliser sans devoir préparer un environnement Python local complexe.

Depuis `API_Workspace` :

```bash
docker compose up --build
```

Une fois la stack démarrée :

- API : `http://localhost:8000`
- Swagger : `http://localhost:8000/docs`
- Adminer : `http://localhost:8080`

Pour repartir d'une base vide en développement :

```bash
docker compose down -v
docker compose up --build
```

Le `-v` supprime le volume PostgreSQL. C'est utile lorsqu'on veut retrouver un état complètement propre, par exemple pour tester un premier lancement du jeu.

---

## 6. Services Docker en détail

### 6.1. Service `api`

Le service `api` build l'application Python à partir de `Src/Dockerfile`.

Le code source est monté depuis `./Src`, ce qui permet de travailler rapidement sur le backend sans reconstruire tout le projet à chaque petite modification.

Variables importantes :

- `DATABASE_URL`
- `PRIVATE_KEY_PATH`
- `GAME_APP_ID`

### 6.2. Service `db`

Le service `db` expose PostgreSQL 16 avec une configuration volontairement simple, adaptée au développement local.

Le volume `pgdata` permet de conserver les données entre deux démarrages tant qu'on ne fait pas explicitement un reset.

### 6.3. Service `adminer`

Adminer est purement un outil de confort.  
Il ne participe pas au fonctionnement du jeu, mais il permet de :

- vérifier qu'un joueur a bien été créé ;
- inspecter les résultats enregistrés ;
- contrôler rapidement l'état de la base.

Connexion :

- Système : `PostgreSQL`
- Serveur : `db`
- Utilisateur : `safechem`
- Mot de passe : `safechem`
- Base : `safechem`

---

## 7. Modèle de données

Le modèle de données actuel est volontairement minimal.  
Il répond au besoin réel du prototype sans introduire de complexité structurelle prématurée.

Deux tables suffisent aujourd'hui :

- `players`
- `level_results`

Le schéma source est documenté dans :

- `API_Workspace/db_mld.puml`

### 7.1. Lecture conceptuelle

#### Table `players`

Cette table porte l'identité du joueur côté backend.

Champs principaux :

- `player_uuid` : identifiant unique transmis par Unity
- `pseudo` : pseudo visible côté jeu
- `created_at` : date de création
- `updated_at` : date de mise à jour
- `deleted_at` : champ réservé pour une éventuelle suppression logique future

Le `player_uuid` vient d'Unity. Il ne dépend pas de PostgreSQL pour être généré.

#### Table `level_results`

Cette table sert à mémoriser les résultats de niveaux remontés par le client.

Champs principaux :

- `id` : clé primaire technique
- `player_uuid` : référence au joueur
- `level_index` : index numérique du niveau
- `level_id` : identifiant logique du niveau
- `duration_seconds` : durée de la partie
- `stars` : nombre d'étoiles obtenu
- `sent_at` : datetime transmis par Unity
- `created_at` : datetime d'enregistrement côté serveur

### 7.2. Relation entre les tables

La relation est simple :

- un joueur peut avoir plusieurs résultats ;
- chaque résultat est rattaché à un seul joueur.

Dans le sens métier, cela reflète bien le besoin du prototype : un profil, puis un historique de tentatives ou de résultats.

### 7.3. Pourquoi ce modèle est suffisant pour le prototype

À ce stade, il n'est pas nécessaire d'ajouter :

- une table de sessions ;
- une table de profils complexes ;
- une table de niveaux maîtres côté backend ;
- une couche analytics dédiée.

Ces besoins pourraient venir plus tard, mais ils n'apporteraient pas de valeur immédiate au prototype actuel.

---

## 8. Vérification des appels entre Unity et l'API

Le backend ne se contente pas de recevoir des requêtes ouvertes.  
Une vérification légère a été mise en place avec une paire de clés RSA.

### 8.1. Répartition des clés

- clé privée serveur : `API_Workspace/Src/keys/private_key.pem`
- clé publique Unity : `Assets/Resources/Security/game_public_key.xml`

### 8.2. Principe

À chaque appel, Unity construit une preuve et l'envoie dans :

- `X-Game-Proof`

Le payload logique correspond à :

```text
safechem-unity-client|<unix_timestamp>
```

Cette preuve est chiffrée côté Unity avec la clé publique.  
L'API la déchiffre avec la clé privée, puis vérifie :

- l'identifiant applicatif attendu ;
- la fraîcheur du timestamp.

### 8.3. Intérêt de cette approche

L'objectif n'est pas d'obtenir une sécurité parfaite de niveau production internet.  
Le but est plutôt de mettre en place une barrière simple et cohérente entre le client du jeu et le backend de prototype.

Cela permet :

- d'éviter des appels arbitraires trop naïfs ;
- de garder une logique compréhensible ;
- de rester aligné avec le niveau de sophistication réellement utile pour ce projet.

---

## 9. Contrat REST

L'API expose peu de routes, ce qui participe à sa lisibilité.

### 9.1. `GET /health`

Route très simple pour vérifier que le service répond.

Usage typique :

- vérifier que l'API est bien démarrée ;
- contrôler rapidement le bon fonctionnement du conteneur.

### 9.2. `POST /players`

Cette route sert à créer le joueur côté backend.

Elle peut aussi jouer le rôle d'upsert léger si le joueur existe déjà.

Exemple de payload :

```json
{
  "player_uuid": "9a9f7e9d5f6b4f7cb3f4d6b7d8e9f001",
  "pseudo": "CatalyseurNova",
  "sent_at": "2026-03-17T12:00:00Z"
}
```

### 9.3. `PATCH /players/{player_uuid}/pseudo`

Cette route met à jour le pseudo du joueur.

Exemple :

```json
{
  "pseudo": "IonSerein",
  "sent_at": "2026-03-17T12:00:00Z"
}
```

### 9.4. `DELETE /players/{player_uuid}?sent_at=...`

Cette route supprime le joueur et ses résultats associés.

Elle est notamment utilisée lorsqu'un reset de progression complet est déclenché côté Unity.

### 9.5. `POST /levels/finished`

Cette route reçoit la fin d'un niveau.

Exemple :

```json
{
  "player_uuid": "9a9f7e9d5f6b4f7cb3f4d6b7d8e9f001",
  "level_index": 2,
  "level_id": "level-2",
  "duration_seconds": 83.2,
  "stars": 3,
  "sent_at": "2026-03-17T12:00:00Z"
}
```

Elle permet d'enregistrer :

- quel niveau a été terminé ;
- en combien de temps ;
- avec quel score final.

---

## 10. Relation concrète avec le projet Unity

Le point d'entrée côté client est :

- `Assets/Scripts/BackendApiClient.cs`

La persistance locale du joueur est gérée par :

- `Assets/Scripts/HomeSceneLayoutFix.cs`

Le déroulé global est le suivant :

1. Unity crée un profil local au premier lancement
2. Unity notifie l'API pour créer le joueur distant
3. Unity met à jour le pseudo si l'utilisateur le modifie
4. Unity envoie les résultats de niveaux lorsqu'une partie se termine
5. Unity supprime puis recrée le joueur distant lors d'un reset complet

Cette articulation est volontairement asymétrique :

- la source de vérité immédiate pour le jeu est locale ;
- le backend joue un rôle de synchronisation et d'archivage léger.

---

## 11. Choix d'implémentation

### 11.1. Pourquoi ne pas avoir séparé en controller / service / repository

Sur un backend de cette taille, une séparation trop poussée aurait ajouté du bruit sans améliorer réellement la maintenabilité.

Le projet gagne davantage à avoir :

- un fichier central compréhensible ;
- peu de points d'entrée ;
- une logique facile à suivre de haut en bas.

Si le backend grandit plus tard, ce découpage pourra être introduit progressivement.

### 11.2. Pourquoi ne pas stocker plus de données

Le prototype n'a pas encore besoin d'un modèle plus large.  
Le backend ne cherche pas à dupliquer toute la structure du jeu Unity.

Il stocke seulement ce qui a un intérêt serveur aujourd'hui :

- l'identité du joueur ;
- son pseudo ;
- les résultats remontés.

### 11.3. Pourquoi conserver un backend optionnel

Le projet Unity doit rester jouable et testable même hors ligne ou sans stack Docker démarrée.

Le backend est donc :

- utile ;
- intégré ;
- mais non bloquant.

Ce choix limite les risques pendant le développement et évite qu'un problème réseau casse la boucle de test gameplay.

---

## 12. Points d'attention pour la suite

Si le projet continue à grandir, les évolutions naturelles côté backend seraient probablement :

- historiser plus finement les tentatives de niveaux ;
- ajouter des endpoints de consultation ;
- mieux distinguer suppression logique et suppression physique ;
- versionner certaines routes si le contrat Unity évolue ;
- découper `main.py` si la surface fonctionnelle devient beaucoup plus large.

Pour l'instant, rien n'impose encore ces évolutions. Le backend remplit correctement son rôle dans le cadre du prototype.

---

## 13. Références utiles

- `API_Workspace/README.md`
- `API_Workspace/db_mld.puml`
- `API_Workspace/docker-compose.yml`
- `API_Workspace/Src/main.py`
- `API_Workspace/Src/Dockerfile`
- `API_Workspace/Src/requirements.txt`
- `API_Workspace/Src/tools/generate_keys.py`
- `Assets/Scripts/BackendApiClient.cs`
- `Assets/Scripts/HomeSceneLayoutFix.cs`

---

## 14. Conclusion

L'API de `SafeChem Lab` a été pensée comme un backend de prototype :

- simple ;
- lisible ;
- cohérent avec Unity ;
- suffisant pour suivre les joueurs et leurs résultats ;
- facile à lancer localement.

Elle ne cherche pas à faire plus que nécessaire, et c'est précisément ce qui fait sa force à ce stade du projet.
