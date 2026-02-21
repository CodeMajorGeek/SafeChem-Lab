# SafeChem Lab

**Formation aux Procédés et à la Sécurité en Industrie Chimique**

Serious game mobile réalisé sous Unity, destiné à la formation à la **prise de décision** et à la **prévention des risques** en industrie chimique. Le joueur planifie des chaînes de transformations chimiques pour obtenir un produit final en respectant les procédures de sécurité, les compatibilités des substances et l’ordre des opérations.

---

## Pitch

*SafeChem Lab* met le joueur dans la peau d’un **opérateur ou technicien** qui doit :

- Planifier des **chaînes de production chimique** (réactifs, procédés, protocoles).
- Respecter les **règles HSE** (Hygiène, Sécurité, Environnement).
- Éviter les **erreurs de séquençage**, les mauvais réactifs et les non-conformités.

L’objectif est de sensibiliser aux dangers (explosion, réaction exothermique, sous-produits toxiques) tout en développant une compréhension des procédés industriels.

**Titre :** *SafeChem* = sécurité chimique, *Lab* = environnement laboratoire / unité de production simulé.

---

## Objectifs pédagogiques

- Identifier les **incompatibilités chimiques** dangereuses.
- Comprendre l’**ordre des opérations** dans un procédé industriel.
- Appliquer les **règles de sécurité** (réactifs, conditions opératoires).
- Prendre des **décisions** en tenant compte des risques et de la conformité aux protocoles.

---

## Public cible

- Étudiants en **génie chimique**, **procédés industriels** ou **sécurité industrielle**.
- **Techniciens** débutants en industrie chimique.
- Âge typique : **18–30 ans**.
- Contexte : formation en centre ou en école, **auto-apprentissage sur smartphone**, sessions courtes (5–15 min).

---

## Structure du jeu (écrans principaux)

Inspiration type *Clash Royale* : navigation par menus, **collection de cartes**, écran de niveaux.

| Écran | Contenu |
|-------|--------|
| **Documentation** | Fiches et rappels : réactifs (substances), procédés, protocoles de sécurité. Consultation des cartes débloquées avec propriétés, dangers, incompatibilités. |
| **Progression** | Liste des **niveaux** (missions de synthèse). Déblocage progressif, objectifs par niveau, scores et étoiles. |
| **Collection** | Toutes les **cartes possédées** : Réactifs, Procédés, Sécurité. Déblocage lié à la progression. |

Dans le prototype actuel, la **navigation** se fait par **swipe** horizontal ou via les **trois boutons du footer** (Documentation, Progression, Collection).

---

## Gameplay (niveaux)

Chaque **niveau** est une mission de synthèse (ex. : acétate d’éthyle) en **3 phases** :

1. **Phase 1 — Réactifs et procédés**  
   Pour chaque étape de la chaîne, le joueur choisit le réactif puis le procédé (verrerie, conditions).

2. **Phase 2 — Protocoles de sécurité**  
   Le joueur associe une carte **protocole de sécurité** à chaque étape (EPI, refroidissement, ordre des opérations, etc.).

3. **Phase 3 — Simulation**  
   Clic sur « Simuler » → déroulement étape par étape avec animations. En cas d’erreur : incident (explosion, surchauffe, fuite) selon le type de faute. Puis **écran de résultat** : score, étoiles, feedback pédagogique.

**Cartes** : Substances (réactifs), Procédés (réactions, distillations, etc.), Sécurité (protocoles). Pièges possibles (mauvais réactif → mauvais produit ou absence de réaction).

---

## Progression et gamification

- **Étoiles** (1 à 3) par niveau : sécurité, ordre logique, nombre d’erreurs, objectifs de temps, rendement.
- **Badges** : Procédé Sûr, Décision Optimale, Vif d’Esprit.
- **Déblocage** : nouveaux niveaux et nouvelles cartes au fil de la progression.
- **Score** : conformité sécurité, respect des protocoles, choix du rendement (meilleur chemin = meilleur score).

---

## Technologie

- **Moteur :** Unity (C#).
- **Plateforme :** Android (prioritaire), iOS optionnel. **Mode portrait**, mobile.
- **Interface :** 2D, Unity UI (uGUI) + TextMeshPro.
- **Données :** modèle data-driven (ScriptableObjects pour substances, procédés, règles).
- **Sauvegarde :** locale ; pas de connexion requise pour le gameplay. API REST optionnelle pour statistiques (pseudonyme).

Référence complète : **Game Design Document** → `Game_Design_Document.pdf` à la racine du projet.

---

## Structure du projet (principale)

```
Assets/
├── Scenes/
│   └── Home.unity              # Écran d’accueil, pager à 3 pages (Doc / Progression / Collection)
├── Scripts/
│   ├── HomePagerSnap.cs        # Swipe et snap entre les 3 pages, événement OnPageChanged
│   ├── HomePagerPageSizer.cs   # Ajustement des pages au viewport
│   └── HomePagerFooterNav.cs   # Sync footer : highlight page active + navigation par boutons
└── Prefabs/
    ├── PageHeader.prefab
    └── PageFooter.prefab
```

Chaque page (Documentation, Progression, Collection) est structurée en **Header** (titre « SafeChem Lab »), **Body** (contenu à venir) et **Footer** (barre de navigation).

---

## Lancement

1. Ouvrir le projet dans **Unity**.
2. Ouvrir la scène **Home** (`Assets/Scenes/Home.unity`).
3. Lancer en **Play** (ou build Android pour test mobile portrait).

---

## Contexte

Projet réalisé dans le cadre du **M2I** (Serious Game).  
Game Design Document : **Game_Design_Document.pdf** (racine du projet).
