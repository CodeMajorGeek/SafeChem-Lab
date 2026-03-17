# SafeChem Lab - Documentation Technique du Projet Unity

## 1. Objectif du document

Ce document décrit l'architecture complète du projet Unity `SafeChem Lab` dans son état actuel, avec un niveau de détail suffisant pour :

- comprendre rapidement la structure du prototype ;
- reprendre le projet sans devoir ré-explorer tout le code ;
- ajouter du contenu data-driven via JSON ;
- identifier les points de couplage entre scènes, UI runtime, persistance locale et backend REST ;
- préparer les prochaines itérations sans casser l'existant.

Le projet est un serious game de chimie basé sur un système de cartes, une progression par niveaux et un ensemble de fiches de support :

- fiches de documentation ;
- cartes de substances ;
- cartes de méthodes/procédés ;
- cartes HSE ;
- niveaux décrits en JSON.

La logique actuelle vise un prototype jouable rapidement itérable, avec une forte part de génération UI/runtime afin de limiter la dépendance à des scènes Unity surchargées.

---

## 2. Vue d'ensemble du projet

### 2.1. Intention produit

`SafeChem Lab` met le joueur dans une logique de sélection et de validation :

- choisir les bonnes substances ;
- choisir la bonne méthode ;
- choisir les bons éléments HSE ;
- obtenir un score final en étoiles selon le temps et les erreurs.

Le projet s'appuie aussi sur un corpus pédagogique :

- documentation chimique ;
- collection consultable de substances ;
- collection consultable de méthodes ;
- collection consultable d'éléments HSE.

### 2.2. Version Unity

Projet actuellement ouvert avec :

- `Unity 6` / `6000.3.6f1`

Fichier de référence :

- `ProjectSettings/ProjectVersion.txt`

### 2.3. Packages Unity notables

Le projet utilise notamment :

- `com.unity.ugui` pour l'UI classique ;
- `com.unity.inputsystem` ;
- `com.unity.textmeshpro` disponible, mais l'UI runtime actuelle utilise principalement `UnityEngine.UI.Text` ;
- `com.unity.render-pipelines.universal` ;
- plusieurs packages 2D et Visual Scripting.

Le prototype repose donc majoritairement sur :

- Canvas + `RectTransform` + `Image` + `Button` + `ScrollRect` ;
- `Resources.Load(...)` pour charger les données et médias ;
- une logique runtime très présente dans les scripts.

---

## 3. Organisation générale du dépôt

### 3.1. Racine

Structure utile à connaître :

```text
SafeChem Lab/
├─ API_Workspace/
├─ Assets/
│  ├─ DocumentationTemplates/
│  ├─ Resources/
│  ├─ Scenes/
│  ├─ Scripts/
│  └─ Settings/
├─ Logs/
├─ Packages/
├─ ProjectSettings/
├─ GameConcept.pdf
├─ Game_Design_Document.pdf
├─ README.md
└─ DOC.md
```

### 3.2. Dossiers importants

#### `Assets/Scripts`

Contient la logique métier et UI runtime du prototype.

Scripts principaux :

- `HomeSceneLayoutFix.cs`
- `InGameRuntimeController.cs`
- `InGameBootstrap.cs`
- `BackendApiClient.cs`
- `HomeDocumentationPanel.cs`
- `HomeSubstancesPanel.cs`
- `HomeMethodsPanel.cs`
- `HomeCollectionSubPagerNav.cs`
- `HomePagerSnap.cs`
- `HomePagerFooterNav.cs`
- `HomePagerPageSizer.cs`
- `RuntimeFileLogger.cs`
- `UiFontProvider.cs`

#### `Assets/Scenes`

Scènes actuellement présentes :

- `Home.unity`
- `InGame.unity`
- `SampleScene.unity`

Dans les faits :

- `Home` est la scène principale d'entrée ;
- `InGame` est la scène de gameplay ;
- `SampleScene` est résiduelle et n'entre pas dans le flux principal actuel.

#### `Assets/Resources`

Le projet est largement piloté par `Resources`.

Sous-dossiers clés :

- `Backgrounds/`
- `Config/`
- `documentation-datas/`
- `Fonts/`
- `HSEs/`
- `Icons/`
- `Levels/`
- `Methods/`
- `Security/`
- `Substances/`
- `substances-datas/` (historique / compatibilité)

#### `API_Workspace`

Ce dossier contient l'API Python + PostgreSQL + Adminer, utilisée par Unity pour :

- créer un joueur ;
- supprimer un joueur ;
- mettre à jour un pseudo ;
- remonter un niveau terminé.

Cette API n'est pas documentée en détail ici, mais sa doc dédiée existe déjà :

- `API_Workspace/DOC.md`

---

## 4. Documents de conception

Les références métier/projet présentes dans le dépôt sont :

- `GameConcept.pdf`
- `Game_Design_Document.pdf`

Le prototype a été aligné progressivement sur ces documents, mais l'état réel du jeu doit être considéré comme la combinaison :

- du code actuel ;
- des JSON présents dans `Assets/Resources` ;
- des scènes `Home` et `InGame`.

En pratique, la vérité d'exécution est dans le code runtime et les fichiers JSON.

---

## 5. Architecture fonctionnelle

Le jeu se découpe aujourd'hui en deux grandes parties :

### 5.1. Home

La scène `Home` regroupe :

- un header fixe ;
- un footer fixe ;
- un pager horizontal de 3 pages ;
- un panneau de paramètres repliable ;
- une splash screen ;
- une modale/drawer de sélection de niveau ;
- la progression ;
- la documentation ;
- la collection.

Les trois pages principales sont :

1. page gauche : documentation ;
2. page centrale : progression ;
3. page droite : collection.

### 5.2. InGame

La scène `InGame` gère :

- l'introduction du niveau ;
- les étapes du niveau ;
- les 3 phases de sélection ;
- la zone de drag & drop ;
- le score final ;
- l'enregistrement local et backend du résultat.

---

## 6. Architecture technique par scène

## 6.1. Scène `Home`

### Script maître : `Assets/Scripts/HomeSceneLayoutFix.cs`

Ce script est le centre de gravité de la scène `Home`.

Responsabilités principales :

- initialiser le profil joueur ;
- normaliser le layout global ;
- construire la progression ;
- construire le sous-pager de collection ;
- gérer la splash screen ;
- gérer le panneau paramètres ;
- gérer le drawer de briefing d'un niveau ;
- lancer la scène `InGame`.

### 6.1.1. Layout global

Le script reconstruit ou recadre une grande partie de l'UI, avec une approche runtime :

- header fixe partagé ;
- footer fixe partagé ;
- fond global fixe ;
- body/pager scrollable uniquement pour le contenu central ;
- hiérarchie réordonnée pour éviter les overlays dans le mauvais ordre.

Le but est d'obtenir un comportement stable sur les 3 pages sans dépendre d'un placement manuel fragile dans l'éditeur.

### 6.1.2. Splash screen

Une coroutine d'ouverture affiche une splash au démarrage.

Elle est gérée dans `HomeSceneLayoutFix` afin d'éviter d'avoir une scène splash séparée.

Cela simplifie :

- l'enchaînement d'entrée ;
- le reset complet après suppression de progression ;
- le flux de lancement dans un prototype.

### 6.1.3. Pager principal

Le pager horizontal repose sur :

- `HomePagerSnap.cs`
- `HomePagerFooterNav.cs`
- `HomePagerPageSizer.cs`

#### `HomePagerSnap.cs`

Responsable de :

- écouter les fins de drag ;
- calculer la page cible ;
- animer le snap ;
- maintenir `CurrentPage` ;
- notifier `OnPageChanged`.

Le pager comporte actuellement 3 pages.

#### `HomePagerFooterNav.cs`

Responsable de :

- relier les 3 boutons du footer au pager ;
- déclencher `SnapToPage(...)` ;
- mettre en surbrillance le bouton actif.

#### `HomePagerPageSizer.cs`

Responsable d'uniformiser les tailles de pages dans le `ScrollRect`.

### 6.1.4. Header et paramètres

Le header contient :

- le logo principal ;
- l'icône d'options.

Le panneau de paramètres :

- se déplie avec animation ;
- occupe environ la moitié de l'écran ;
- se replie en cliquant à l'extérieur ;
- permet de modifier le pseudo ;
- affiche l'identité joueur ;
- propose un bouton de reset de progression ;
- ouvre une modale de confirmation avant suppression.

### 6.1.5. Reset progression

Depuis les paramètres, le bouton rouge de reset :

1. supprime l'identité et la progression courante côté local ;
2. supprime aussi le joueur côté backend ;
3. recrée un nouveau profil avec UUID et pseudo aléatoire ;
4. recharge le jeu depuis la scène `Home` et donc repasse par la splash screen.

Cette logique permet de retrouver un comportement de "premier lancement".

### 6.1.6. Page progression

La page centrale charge les niveaux via les JSON de :

- `Assets/Resources/Levels/`

Le mapping visuel actuel :

- bouton de niveau ;
- label `Niveau n` ;
- étoiles au-dessus ;
- état verrouillé/déverrouillé ;
- drawer coulissant en bas avec briefing et bouton `Jouer`.

#### Règle de déverrouillage

La logique actuelle est :

- le niveau 1 est débloqué ;
- chaque niveau suivant est débloqué si le niveau précédent a obtenu au moins 1 étoile.

#### Drawer de niveau

Quand un niveau débloqué est cliqué :

- les infos sont copiées dans des clés `PlayerPrefs` de sélection courante ;
- un drawer s'ouvre en bas ;
- le joueur voit un résumé ;
- le bouton `Jouer` charge la scène déclarée dans le JSON du niveau.

### 6.1.7. Page documentation

Gérée par `Assets/Scripts/HomeDocumentationPanel.cs`.

Responsabilités :

- charger toutes les fiches JSON dans `Resources/documentation-datas` ;
- construire une liste de boutons ;
- ouvrir une modale plein écran ;
- afficher le contenu formaté de la fiche ;
- gérer la fermeture.

Particularités :

- le rendu de texte supporte un mini formatage simple ;
- certaines syntaxes type `**gras**` et `__gras__` sont converties vers le rich text Unity ;
- la modale passe au-dessus du reste de l'interface.

### 6.1.8. Page collection

La page droite est elle-même subdivisée en 3 sous-pages :

1. Substances
2. Methods
3. HSE

La navigation est gérée par `Assets/Scripts/HomeCollectionSubPagerNav.cs`.

Ce script :

- active/désactive les sous-pages ;
- gère la ligne de 3 boutons sous le header ;
- charge les icônes ;
- met en évidence l'onglet courant.

#### Sous-page substances

Gérée par `Assets/Scripts/HomeSubstancesPanel.cs`.

Responsabilités :

- charger les substances depuis JSON ;
- extraire des sprites depuis une texture atlas ;
- construire une grille de cartes ;
- ouvrir une modale détaillée.

#### Sous-page methods

Gérée par `Assets/Scripts/HomeMethodsPanel.cs`.

Responsabilités proches du panneau substances, mais pour les procédés :

- chargement JSON ;
- extraction atlas ;
- grille ;
- modale de détail.

#### Sous-page HSE

Le fonctionnement est aligné sur le même principe data-driven que les substances et méthodes, avec son dossier et son atlas dédiés.

---

## 6.2. Scène `InGame`

### Scripts maîtres

- `Assets/Scripts/InGameBootstrap.cs`
- `Assets/Scripts/InGameRuntimeController.cs`

### 6.2.1. `InGameBootstrap.cs`

Ce script sert de point d'entrée runtime pour la scène `InGame`.

Responsabilités :

- vérifier que la scène active est bien `InGame` ;
- garantir la présence d'un `EventSystem` ;
- instancier un objet racine `InGameRuntimeRoot` ;
- y attacher `InGameRuntimeController`.

Autrement dit, la scène `InGame` sert de conteneur léger, et l'UI de gameplay est reconstruite par script.

### 6.2.2. `InGameRuntimeController.cs`

C'est le coeur du gameplay actuel.

Responsabilités :

- charger le niveau sélectionné ;
- charger ses étapes ;
- construire toute l'UI de jeu ;
- gérer les phases ;
- gérer le drag & drop ;
- calculer les erreurs ;
- calculer les étoiles ;
- enregistrer le résultat.

### 6.2.3. Chargement du niveau

Le niveau courant est recherché via :

- l'identifiant sélectionné stocké dans `PlayerPrefs` depuis `Home` ;
- sinon un fallback vers `Levels/level-X` ;
- sinon un fallback final vers `level-1`.

Le niveau est chargé depuis :

- `Resources/Levels/<id>`

### 6.2.4. Phases de gameplay

Le jeu repose sur 3 phases séquentielles :

1. sélection des substances ;
2. sélection de la méthode ;
3. sélection des cartes HSE.

Les phases sont représentées par l'énumération :

- `InGamePhase.SubstanceSelection`
- `InGamePhase.MethodSelection`
- `InGamePhase.HseSelection`
- `InGamePhase.Completed`

### 6.2.5. Étapes de niveau

Un niveau peut contenir plusieurs étapes.

Chaque étape définit :

- son texte d'introduction ;
- ses nombres de slots ;
- ses textes d'instruction par phase ;
- les cartes à afficher ;
- les cartes attendues ;
- les pièges.

Aujourd'hui, les niveaux 1 et 2 sont réellement renseignés avec une étape jouable.

### 6.2.6. UI de gameplay

L'UI générée runtime comprend :

- un fond fixe ;
- un bandeau haut avec timer + titre + instruction de phase ;
- une zone centrale de slots ;
- un bouton `Valider` ;
- un tray de cartes en bas ;
- un écran d'introduction ;
- un écran de résultat.

### 6.2.7. Drag & drop

Deux classes runtime structurent cette mécanique :

#### `InGameDropSlot`

Rôle :

- représenter un slot cible ;
- vérifier si la phase active autorise le dépôt ;
- gérer les états visuels ;
- gérer le survol ;
- gérer le rejet visuel d'une mauvaise action.

#### `InGameDraggableCard`

Rôle :

- représenter une carte draggable ;
- mémoriser sa position d'origine ;
- suivre le curseur ;
- revenir à la base si nécessaire ;
- se verrouiller après validation ;
- changer légèrement de layout quand elle est dans un slot.

### 6.2.8. Validation d'une phase

Le bouton `Valider` n'oblige plus à remplir tous les slots.

Comportement actuel :

- le joueur peut valider même avec 0 ou plusieurs slots vides ;
- les cartes manquantes comptent comme erreurs ;
- les cartes pièges comptent comme erreurs ;
- la transition vers la phase suivante conserve l'affichage des cartes déjà choisies.

### 6.2.9. Calcul des erreurs et des étoiles

La logique actuelle est la suivante :

- si `errors > 0` -> `1 étoile`
- si `errors == 0` et `temps < 60 s` -> `3 étoiles`
- si `errors == 0` et `temps >= 60 s` -> `2 étoiles`

Le calcul est centralisé dans `ComputeStars(...)`.

### 6.2.10. Résultat de fin de niveau

En fin de niveau :

- le timer s'arrête ;
- le score est calculé ;
- les étoiles sont affichées ;
- le meilleur score local est enregistré ;
- l'événement de fin de niveau est envoyé au backend.

La scène de résultat propose notamment un retour vers le menu principal.

---

## 7. Système de données piloté par JSON

Le projet a été structuré pour que l'ajout de contenu passe prioritairement par des fichiers JSON et des assets Unity placés au bon endroit.

### 7.1. Documentation

Dossier :

- `Assets/Resources/documentation-datas/`

Contenu actuellement présent :

- `diels-alder.json`
- `double-displacement.json`
- `esterification-fischer.json`

Template :

- `Assets/DocumentationTemplates/documentation-template.json`

Structure logique :

- `id`
- `title`
- `category`
- `shortDescription`
- `whyItMatters`
- `sections[]`
- `keywords[]`

### 7.2. Substances

Dossier :

- `Assets/Resources/Substances/`

Exemples présents :

- `ethanol.json`
- `acide-acetique.json`
- `h2so4.json`
- `hcl.json`
- `kno3.json`
- `hno3.json`
- `mgso4.json`
- `cuso4.json`
- `csso4.json`
- `znso4.json`
- `fes.json`
- `acetate-ethyl.json`

Template :

- `Assets/Resources/Substances/substance-template.json`

Texture atlas actuelle :

- `Assets/Resources/Substances/substances-1.png`

Champs importants :

- `id`
- `displayName`
- `formula`
- `shortDescription`
- `hazardSummary`
- `handlingNotes`
- `atlasTextureResource`
- `atlasCellIndex`
- `atlasColumns`
- `atlasCellWidth`
- `atlasCellHeight`
- `moleculeImageResource`
- `cardImageResource`
- `tags`

Le système permet d'extraire dynamiquement une carte depuis un atlas.

### 7.3. Methods

Dossier :

- `Assets/Resources/Methods/`

Contenu présent :

- `becher-melangeur.json`
- `distillation.json`
- `reflux-froid.json`
- `colonne-chroma.json`
- `filtration-gravite.json`

Template :

- `Assets/Resources/Methods/method-template.json`

Texture atlas actuelle :

- `Assets/Resources/Methods/verrie-1.png`

Champs importants :

- `id`
- `title`
- `subtitle`
- `shortDescription`
- `detailedDescription`
- `safetyNotes`
- `atlasTextureResource`
- `atlasX`
- `atlasY`
- `atlasWidth`
- `atlasHeight`
- `imageResource`
- `tags`

### 7.4. HSE

Dossier :

- `Assets/Resources/HSEs/`

Contenu présent :

- `corrosif.json`
- `explosif.json`
- `inflammable.json`
- `toxique.json`

Template :

- `Assets/Resources/HSEs/hse-template.json`

Texture atlas actuelle :

- `Assets/Resources/HSEs/HSE-2.png`

Champs importants :

- `id`
- `title`
- `subtitle`
- `shortDescription`
- `detailedDescription`
- `safetyNotes`
- `atlasTextureResource`
- `atlasX`
- `atlasY`
- `atlasWidth`
- `atlasHeight`
- `imageResource`
- `tags`

### 7.5. Niveaux

Dossier :

- `Assets/Resources/Levels/`

Contenu présent :

- `level-0.json`
- `level-1.json`
- `level-2.json`
- `level-3.json`
- `level-4.json`
- `level-5.json`
- `level-6.json`

Template :

- `Assets/Resources/Levels/level-template.json`

Champs de niveau importants :

- `id`
- `levelIndex`
- `title`
- `summary`
- `objective`
- `hseFocus`
- `sceneName`
- `levelBrief`
- `targetProductId`
- `targetProductName`
- `timeTargetSeconds`
- `defaultSubstanceSlotCount`
- `defaultMethodSlotCount`
- `defaultHseSlotCount`
- `substanceIds[]`
- `methodIds[]`
- `hseIds[]`
- `steps[]`

Champs importants d'une étape :

- `stepId`
- `stepTitle`
- `stepBrief`
- `introBrief`
- `introObjective`
- `introHseFocus`
- `substanceSlotCount`
- `methodSlotCount`
- `hseSlotCount`
- `substancePhaseInstruction`
- `methodPhaseInstruction`
- `hsePhaseInstruction`
- `displaySubstanceIds[]`
- `displayMethodIds[]`
- `displayHseIds[]`
- `expectedSubstanceIds[]`
- `expectedMethodIds[]`
- `expectedHseIds[]`
- `trapSubstanceIds[]`
- `trapMethodIds[]`
- `trapHseIds[]`

### 7.6. État réel du contenu niveaux

Au moment de cette documentation :

- `level-1.json` est jouable et détaillé ;
- `level-2.json` est jouable et détaillé ;
- `level-0.json`, `level-3.json`, `level-4.json`, `level-5.json`, `level-6.json` existent mais restent des placeholders fonctionnels ou semi-renseignés.

Cela signifie que la structure est prête pour ajouter du contenu, mais que tous les niveaux ne sont pas encore entièrement designés au même niveau de finition.

---

## 8. Persistance locale joueur

La persistance locale est gérée dans `PlayerProfileStore`, classe incluse à la fin de :

- `Assets/Scripts/HomeSceneLayoutFix.cs`

### 8.1. Données stockées

Structure du profil :

- `playerUuid`
- `pseudo`
- `createdAtUtc`
- `updatedAtUtc`
- `levelResults[]`

Structure d'un résultat de niveau :

- `levelId`
- `levelIndex`
- `bestStars`
- `bestTimeSeconds`
- `bestErrors`
- `completionCount`
- `lastCompletedUtc`

### 8.2. Stockage disque

Le profil est enregistré dans :

- `Application.persistentDataPath/player-profile.json`

Cette sauvegarde est la source de vérité locale principale.

### 8.3. Premier lancement

Au premier lancement :

- si aucun profil valide n'existe, un profil est créé ;
- un UUID aléatoire est généré ;
- un pseudo aléatoire est choisi ;
- l'ensemble est immédiatement sauvegardé.

### 8.4. Pseudos par défaut

Le catalogue est dans :

- `Assets/Resources/Config/default-pseudos.json`

Il contient actuellement 30 pseudos thématiques liés à la chimie.

### 8.5. Compatibilité legacy

Le store synchronise encore certaines informations dans `PlayerPrefs`, notamment :

- pseudo ;
- étoiles par niveau.

But :

- conserver la compatibilité avec d'anciens flux déjà présents dans le prototype ;
- éviter une cassure brutale pendant la transition vers un stockage JSON local plus propre.

---

## 9. Intégration backend REST

L'intégration réseau Unity est portée par :

- `Assets/Scripts/BackendApiClient.cs`

### 9.1. Rôle

Le client envoie des événements simples vers l'API locale :

- création de player ;
- suppression de player ;
- mise à jour du pseudo ;
- niveau terminé.

### 9.2. Mode de fonctionnement

Le client :

- pointe par défaut vers `http://127.0.0.1:8000` ;
- utilise `HttpClient` ;
- envoie les requêtes en fire-and-forget ;
- ne bloque pas la boucle de jeu ;
- log les erreurs mais ne casse pas le gameplay si l'API ne répond pas.

### 9.3. Preuve de jeu

Le projet charge une clé publique Unity depuis :

- `Assets/Resources/Security/game_public_key.xml`

Le client chiffre un payload de preuve envoyé dans le header :

- `X-Game-Proof`

L'API Python vérifie cette preuve avec sa clé privée correspondante.

### 9.4. Déclencheurs côté Unity

Les appels backend se produisent notamment lors de :

- la création initiale du profil ;
- la modification du pseudo ;
- le reset progression ;
- la fin d'un niveau.

---

## 10. Logging et debug

Le logging runtime est géré par :

- `Assets/Scripts/RuntimeFileLogger.cs`

### 10.1. Emplacement

Le fichier est écrit dans :

- `Logs/safechem-runtime.log`

### 10.2. Usage

Le logger est utilisé pour :

- les erreurs de chargement JSON ;
- les warnings de ressources manquantes ;
- les problèmes API ;
- certaines traces de cycle de vie.

Le logger est particulièrement utile lorsque l'inspecteur Unity ne suffit pas à comprendre un problème de chargement runtime.

---

## 11. Gestion des médias et polices

### 11.1. Polices

Le fournisseur central de police est :

- `Assets/Scripts/UiFontProvider.cs`

Ordre de fallback :

1. `Resources/Fonts/LiberationSans`
2. police OS `Arial`
3. police OS `Segoe UI`

### 11.2. Sprites et atlases

Le projet mélange plusieurs stratégies :

- sprites chargés directement depuis `Resources` ;
- découpe de textures atlas à partir de coordonnées JSON ;
- fallback entre `Texture2D`, `Sprite`, ou `Resources.LoadAll<Sprite>()`.

Cela donne de la souplesse, mais impose de respecter strictement :

- les chemins `Resources` ;
- les noms de fichiers ;
- les dimensions/coordonnées d'atlas.

---

## 12. Scripts principaux et responsabilités

### `Assets/Scripts/HomeSceneLayoutFix.cs`

Rôle :

- orchestrateur principal de la scène `Home` ;
- layout global ;
- paramètres ;
- progression ;
- drawer de briefing ;
- splash screen ;
- store profil joueur.

### `Assets/Scripts/HomeDocumentationPanel.cs`

Rôle :

- chargement des docs JSON ;
- génération de boutons ;
- ouverture de modales ;
- rendu texte formaté.

### `Assets/Scripts/HomeSubstancesPanel.cs`

Rôle :

- chargement des cartes substances ;
- extraction atlas ;
- grille de cartes ;
- modale de détail.

### `Assets/Scripts/HomeMethodsPanel.cs`

Rôle :

- équivalent du panneau substances, mais pour les procédés.

### `Assets/Scripts/HomeCollectionSubPagerNav.cs`

Rôle :

- navigation interne de la page collection ;
- activation des sous-pages ;
- mise à jour des icônes et états de boutons.

### `Assets/Scripts/HomePagerSnap.cs`

Rôle :

- snap horizontal du pager principal 3 pages.

### `Assets/Scripts/HomePagerFooterNav.cs`

Rôle :

- navigation du footer ;
- surbrillance page active.

### `Assets/Scripts/HomePagerPageSizer.cs`

Rôle :

- uniformisation des tailles des pages du pager.

### `Assets/Scripts/InGameBootstrap.cs`

Rôle :

- bootstrap runtime de la scène `InGame`.

### `Assets/Scripts/InGameRuntimeController.cs`

Rôle :

- moteur de gameplay runtime ;
- slots ;
- phases ;
- drag & drop ;
- score ;
- résultat ;
- sauvegarde de fin de niveau.

### `Assets/Scripts/BackendApiClient.cs`

Rôle :

- communication REST sortante vers le backend Python.

### `Assets/Scripts/RuntimeFileLogger.cs`

Rôle :

- journalisation runtime sur disque.

### `Assets/Scripts/UiFontProvider.cs`

Rôle :

- fournir une police par défaut robuste côté UI runtime.

---

## 13. Workflow d'ajout de contenu

## 13.1. Ajouter une documentation

1. Copier `Assets/DocumentationTemplates/documentation-template.json`
2. Créer un nouveau fichier dans `Assets/Resources/documentation-datas/`
3. Renseigner les champs de la fiche
4. Laisser Unity générer/importer le `.meta`
5. Relancer la scène `Home`

La page documentation charge automatiquement tous les JSON valides du dossier.

## 13.2. Ajouter une substance

1. Ajouter ou réutiliser une case dans l'atlas `Substances`
2. Créer un JSON dans `Assets/Resources/Substances/`
3. Renseigner les données d'affichage et l'index/cellule atlas
4. Vérifier l'apparition dans la sous-page substances
5. Référencer l'`id` de la substance dans un ou plusieurs niveaux

## 13.3. Ajouter une méthode

1. Ajouter la texture ou la zone atlas dans `Methods/verrie-1.png` ou un futur atlas
2. Créer un JSON dans `Assets/Resources/Methods/`
3. Décrire les coordonnées atlas
4. Vérifier le rendu en collection
5. Référencer l'`id` dans un niveau

## 13.4. Ajouter un élément HSE

1. Ajouter la texture dans l'atlas HSE
2. Créer le JSON dans `Assets/Resources/HSEs/`
3. Vérifier l'affichage en collection
4. Référencer l'`id` dans les niveaux concernés

## 13.5. Ajouter un niveau

1. Copier `Assets/Resources/Levels/level-template.json`
2. Créer `level-X.json`
3. Définir les champs de haut niveau
4. Définir au moins une `step`
5. Renseigner :
   - les cartes affichées ;
   - les cartes attendues ;
   - les pièges ;
   - les instructions ;
   - le nombre de slots.
6. Vérifier que la progression l'affiche correctement dans `Home`
7. Vérifier qu'un clic sur le niveau ouvre bien `InGame`

Important :

- le dossier `Levels` est scanné automatiquement ;
- les fichiers `template` sont ignorés ;
- `levelIndex` doit être cohérent.

---

## 14. Conventions importantes

### 14.1. Chargement via `Resources`

Le projet dépend fortement de `Resources.Load(...)`.

Conséquence :

- ne pas déplacer ou renommer arbitrairement les fichiers data et médias ;
- garder des chemins stables ;
- éviter d'avoir plusieurs assets ambigus sur le même nom logique.

### 14.2. `.meta` Unity

Le prototype a déjà subi plusieurs itérations d'import UI et d'assets.

Pour rester stable :

- laisser Unity gérer les `.meta` après déplacement/import ;
- éviter les suppressions manuelles hasardeuses ;
- si un atlas ou sprite "disparaît", vérifier d'abord le `.meta`, le mode d'import et le chemin `Resources`.

### 14.3. Runtime UI

Une part importante de l'UI est générée en code plutôt qu'en prefab.

Avantages :

- itération rapide ;
- reproductibilité ;
- moins de dépendance à une scène Unity fragile.

Inconvénients :

- plus de responsabilités concentrées dans certains scripts longs ;
- besoin d'une documentation claire sur les conventions.

---

## 15. État actuel du prototype

### 15.1. Ce qui est déjà structuré

- menu home à 3 écrans ;
- header/footer fixes ;
- panneau paramètres ;
- reset de progression ;
- stockage local du joueur ;
- communication REST avec backend ;
- documentation data-driven ;
- collection data-driven substances/methods/HSE ;
- progression avec verrouillage ;
- au moins 2 niveaux réellement jouables ;
- gameplay drag & drop multi-phase ;
- score en étoiles ;
- logs runtime.

### 15.2. Ce qui reste naturellement extensible

- ajouter d'autres niveaux complets ;
- enrichir la documentation ;
- enrichir le catalogue de substances ;
- enrichir les procédés ;
- enrichir les cartes HSE ;
- raffiner encore certaines scènes/prefabs si souhaité ;
- migrer progressivement certains blocs runtime vers des prefabs si le besoin apparaît.

### 15.3. Dette technique raisonnable identifiée

- `HomeSceneLayoutFix.cs` concentre beaucoup de responsabilités ;
- `InGameRuntimeController.cs` est volumineux car il regroupe tout le runtime gameplay ;
- le projet utilise encore `Resources` partout, ce qui reste pratique pour un prototype mais moins scalable qu'une architecture adressable ou outillée ;
- certaines données plus anciennes montrent des traces d'encodage texte à surveiller selon l'origine des fichiers.

Cette dette reste acceptable pour un prototype de serious game en itération rapide, à condition de garder les conventions de structure et de contenu.

---

## 16. Recommandations pour la suite

### 16.1. Pour continuer proprement

- continuer à privilégier l'ajout de contenu via JSON ;
- garder des identifiants simples et stables ;
- conserver la logique de scènes légère + runtime UI pour les écrans encore très mouvants ;
- documenter chaque nouvel atlas et son découpage ;
- tester chaque nouveau niveau sur les 3 axes :
  - chargement JSON ;
  - rendu des cartes ;
  - calcul du score.

### 16.2. Pour solidifier le projet plus tard

- sortir `PlayerProfileStore` dans son propre fichier ;
- découper `HomeSceneLayoutFix` en modules plus spécialisés ;
- découper `InGameRuntimeController` par sous-systèmes ;
- fiabiliser l'encodage UTF-8 de tous les JSON ;
- envisager des prefabs réutilisables pour certaines cartes et modales si la UI se stabilise.

---

## 17. Références rapides

### Scènes

- `Assets/Scenes/Home.unity`
- `Assets/Scenes/InGame.unity`

### Scripts centraux

- `Assets/Scripts/HomeSceneLayoutFix.cs`
- `Assets/Scripts/InGameRuntimeController.cs`
- `Assets/Scripts/InGameBootstrap.cs`
- `Assets/Scripts/BackendApiClient.cs`

### Data

- `Assets/Resources/Levels/`
- `Assets/Resources/Substances/`
- `Assets/Resources/Methods/`
- `Assets/Resources/HSEs/`
- `Assets/Resources/documentation-datas/`

### Config

- `Assets/Resources/Config/default-pseudos.json`
- `Assets/Resources/Security/game_public_key.xml`

### Documents projet

- `GameConcept.pdf`
- `Game_Design_Document.pdf`

### Backend lié

- `API_Workspace/DOC.md`

---

## 18. Conclusion

Le projet Unity `SafeChem Lab` est aujourd'hui un prototype structuré autour d'une idée forte :

- un coeur de gameplay simple ;
- un maximum de contenu piloté par JSON ;
- une UI largement générée/runtime pour itérer vite ;
- une persistance locale claire ;
- une passerelle backend REST légère.

La base technique est suffisamment propre pour continuer l'implémentation du prototype, à condition de conserver les conventions actuelles :

- mêmes dossiers ;
- mêmes chemins `Resources` ;
- mêmes schémas JSON ;
- même séparation `Home` / `InGame`.

Ce document doit servir de référence de reprise pour tout le reste du développement.
