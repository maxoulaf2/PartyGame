# E18 — Enchaînement des manches

**Phase :** 6. Une vraie soirée
**Objectif :** qu'une soirée de plusieurs manches de modes différents ait un rythme. Chaque manche s'ouvre sur un écran qui l'annonce et en rappelle la règle, et les classements entretiennent le suspense jusqu'au podium.

## Déjà en place

Les packs multi-manches de modes différents, l'enchaînement des manches par le GM (US-E07-01), le classement entre deux manches (US-E09-02) et le classement final avec son podium (US-E09-03) existent. Cette épopée ajoute l'introduction des manches et l'animation des classements.

## User stories

| US | Titre | Statut | Dépend de |
|---|---|---|---|
| [US-E18-01](US-E18-01-introduction-de-manche.md) | Écran d'introduction de chaque manche | À faire | — |
| [US-E18-02](US-E18-02-classements-animes.md) | Classements animés et podium révélé marche par marche | À faire | — |

## Décisions

Toutes les décisions qui bloquaient l'épopée sont prises (2026-10-06).

1. **Jeu en individuel seulement :** pas d'équipes. Les scores, les classements et le podium restent individuels. Le jeu en équipes rejoint les idées non planifiées de la roadmap. Options écartées : des équipes facultatives formées par le GM dans le lobby, et un classement par équipe affiché à côté du classement individuel.
2. **Introduction pilotée par le GM :** « Manche suivante » ouvre l'écran d'introduction, et c'est un second appui, « Commencer », qui démarre la manche. Le GM laisse ainsi le temps d'expliquer la règle à voix haute. Choix de réalisation, ajustable sans nouvelle décision.
3. **Règle rappelée par le client :** le texte de règle de chaque mode est un texte d'interface, écrit dans `fr.ts` pour chaque type de mode. Le serveur n'envoie que le type de la manche et, le cas échéant, la description libre écrite par l'auteur du pack (`description`). Choix de réalisation, ajustable sans nouvelle décision.
4. **Animations calculées par la TV à partir du snapshot :** l'évolution des rangs est calculée par le moteur (rang à la manche précédente) ; la TV n'en fait que l'animation. La révélation du podium marche par marche est une séquence fixe jouée par la TV, sans intention GM. Choix de réalisation, ajustable sans nouvelle décision.

## Ordre de réalisation suggéré

1. US-E18-01.
2. US-E18-02, avant le thème visuel de E20, qui en reprend les animations.

## Critère de sortie (phase 6)

Voir le README de [E20](../E20-habillage/README.md#critère-de-sortie-phase-6).
