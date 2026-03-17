# SafeChem Lab

`SafeChem Lab` est un serious game mobile réalisé sous Unity autour de la chimie, des procédés et de la sécurité en laboratoire.  
Le projet place le joueur dans une logique de choix : sélectionner les bonnes substances, le bon procédé, les bons éléments HSE, puis valider un montage cohérent pour atteindre un objectif de synthèse.

Ce dépôt contient à la fois :

- le projet Unity ;
- les données JSON qui alimentent le contenu ;
- le backend REST local utilisé pour synchroniser le profil joueur et les résultats de niveaux.

---

## L'idée du projet

Le coeur de `SafeChem Lab` est assez simple à comprendre : apprendre en jouant à prendre de meilleures décisions en contexte chimique.

Le joueur navigue dans trois grands espaces :

- une zone de documentation ;
- une progression par niveaux ;
- une collection de cartes liées aux substances, aux méthodes et au HSE.

Chaque niveau demande ensuite de faire des choix concrets :

- quelles substances utiliser ;
- quel procédé choisir ;
- quels pictogrammes ou éléments HSE associer ;
- comment éviter les pièges.

Le projet vise donc autant la logique de raisonnement que la mémorisation brute.

---

## État actuel du prototype

Le prototype est déjà structuré autour de deux scènes principales :

- `Home` pour l'accueil, la progression, la documentation et la collection ;
- `InGame` pour le déroulé d'un niveau.

Aujourd'hui, le projet comprend notamment :

- un menu principal à trois écrans avec header et footer fixes ;
- une documentation chargée depuis des fichiers JSON ;
- une collection de substances, méthodes et cartes HSE chargées depuis JSON ;
- une progression par niveaux avec verrouillage et système d'étoiles ;
- un gameplay de drag & drop dans la scène `InGame` ;
- une persistance locale du profil joueur ;
- une synchronisation backend légère via API REST.

Le tout a été pensé pour rester très data-driven : une grande partie du contenu peut être ajoutée ou modifiée sans devoir reconstruire toute l'interface.

---

## Comment le projet est organisé

Quelques dossiers donnent tout de suite la structure du dépôt :

```text
SafeChem Lab/
├─ API_Workspace/
├─ Assets/
│  ├─ Resources/
│  ├─ Scenes/
│  └─ Scripts/
├─ Packages/
├─ ProjectSettings/
├─ GameConcept.pdf
├─ Game_Design_Document.pdf
├─ README.md
└─ DOC.md
```

### `Assets/Scenes`

On y trouve les scènes principales du jeu :

- `Home.unity`
- `InGame.unity`

### `Assets/Scripts`

Ce dossier porte l'essentiel de la logique runtime.

Les scripts les plus structurants aujourd'hui sont :

- `HomeSceneLayoutFix.cs`
- `InGameRuntimeController.cs`
- `InGameBootstrap.cs`
- `BackendApiClient.cs`

### `Assets/Resources`

Le contenu pédagogique et une partie importante du contenu jouable sont chargés depuis `Resources`.

On y retrouve notamment :

- `Levels/`
- `Substances/`
- `Methods/`
- `HSEs/`
- `documentation-datas/`
- `Config/`
- `Security/`

### `API_Workspace`

Ce dossier contient le backend Python local utilisé pour suivre les joueurs et les résultats de niveaux.  
Il dispose de sa propre documentation :

- `API_Workspace/README.md`
- `API_Workspace/DOC.md`

---

## Philosophie technique

Le projet a été construit avec une priorité claire : avancer vite sans perdre complètement la structure.

Cela se traduit par plusieurs choix forts :

- beaucoup d'UI générée ou ajustée en runtime ;
- un maximum de contenu déplacé dans des JSON ;
- une logique de persistance locale simple ;
- un backend volontairement minimaliste ;
- un découpage qui reste pragmatique pour un prototype.

Ce n'est pas une architecture pensée pour un très grand projet live-service.  
En revanche, pour un serious game en cours d'itération, elle permet d'ajouter rapidement :

- de nouvelles fiches ;
- de nouvelles cartes ;
- de nouveaux niveaux ;
- de nouveaux visuels ;
- de nouvelles règles de progression.

---

## Lancement du projet Unity

1. Ouvrir le projet dans Unity
2. Ouvrir la scène `Assets/Scenes/Home.unity`
3. Lancer le mode Play

Version Unity actuellement utilisée :

- `Unity 6` / `6000.3.6f1`

Le projet cible un usage mobile en orientation portrait.

---

## Lancement du backend local

Depuis le dossier `API_Workspace` :

```bash
docker compose up --build
```

URLs utiles :

- API : `http://localhost:8000`
- Swagger : `http://localhost:8000/docs`
- Adminer : `http://localhost:8080`

Le gameplay Unity reste d'abord local, mais l'API permet de synchroniser :

- la création du joueur ;
- la mise à jour du pseudo ;
- la suppression/reset du profil ;
- la fin d'un niveau.

---

## Où regarder pour comprendre le projet en détail

Si tu veux une vision technique plus complète, les documents à ouvrir en priorité sont :

- `DOC.md` pour la documentation technique du projet Unity ;
- `API_Workspace/README.md` pour une vue rapide du backend ;
- `API_Workspace/DOC.md` pour la documentation technique détaillée de l'API ;
- `Game_Design_Document.pdf` pour l'intention design ;
- `GameConcept.pdf` pour le cadrage initial.

---

## À retenir

`SafeChem Lab` est aujourd'hui un prototype déjà bien structuré :

- Unity gère l'expérience de jeu ;
- les JSON pilotent une grande partie du contenu ;
- l'API complète le projet sans le rendre dépendant du réseau ;
- la base actuelle est suffisamment claire pour continuer à ajouter du contenu proprement.

Le dépôt a donc été pensé pour être repris, enrichi et itéré sans devoir réécrire tout le socle à chaque ajout.
