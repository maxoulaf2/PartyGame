# E16 — Mode question ouverte

**Phase :** 5. Questions ouvertes et contenu
**Objectif :** des questions sans propositions. Chaque joueur tape sa réponse sur son téléphone avant la fin d'un compte à rebours, le serveur pré-classe les réponses, le GM valide le lot d'un coup d'œil, et la TV révèle la bonne réponse avec tout ce que les joueurs ont proposé.

## User stories

| US | Titre | Statut | Dépend de |
|---|---|---|---|
| [US-E16-01](US-E16-01-descripteur-question-ouverte.md) | Descripteur d'une manche de questions ouvertes | Terminée | — |
| [US-E16-02](US-E16-02-question-et-saisie.md) | Question posée et saisie sur le téléphone | Terminée | US-E16-01 |
| [US-E16-03](US-E16-03-pre-classement-et-validation.md) | Pré-classement et validation en lot par le GM | À faire | US-E16-02 |
| [US-E16-04](US-E16-04-revelation-et-points.md) | Révélation et points | À faire | US-E16-03 |

## Décisions

Toutes les décisions qui bloquaient l'épopée sont prises (2026-10-05). Les valeurs numériques ci-dessous sont des valeurs de départ, ajustables sans nouvelle décision.

1. **Compte à rebours et réponse définitive :** comme au quiz (décisions 2 et 3 du README de E08), chaque manche a une durée de réponse par défaut (`answerSeconds`, 30 s si absente, de 5 à 120 s), surchargeable par question. Un joueur n'envoie qu'une réponse : une fois envoyée, il ne peut plus la corriger. Les réponses se verrouillent dès que tous les participants ont répondu, sinon à la fin du compte à rebours. Options écartées : une réponse modifiable jusqu'à l'échéance, et une saisie sans délai close par le GM.
2. **Pré-classement en suggestions pré-cochées :** le serveur range chaque réponse dans une catégorie, « acceptée » (égale à la réponse attendue ou à une variante, après normalisation), « à vérifier » (proche, d'après la distance de Levenshtein) ou « refusée ». La console GM arrive avec les réponses acceptées cochées ; le GM corrige ce qu'il veut et valide tout le lot en une fois. Options écartées : de simples indications sans rien de coché, et une validation automatique où le GM ne tranche que les réponses proches.
3. **Barème du quiz :** une réponse acceptée rapporte les points de la manche (`points`, 1 000 par défaut), plus un bonus de rapidité facultatif (`speedBonus`, 0 par défaut) en proportion du temps restant à la réception de la réponse (décision 1 du README de E09). Une réponse refusée ou absente rapporte 0. Les points sont attribués à la révélation. Options écartées : des points fixes sans bonus possible, et des points partiels accordés par le GM.
4. **Toutes les réponses révélées, avec leurs auteurs :** à la révélation, la TV affiche la bonne réponse, puis toutes les réponses reçues, regroupées (les réponses identiques après normalisation n'en font qu'une, suivie des pseudos de leurs auteurs) et séparées en bonnes et mauvaises. Options écartées : des réponses notables choisies par le GM, avec ou sans leurs auteurs.
5. **Question affichée à l'ouverture des réponses :** la TV montre d'abord le numéro de la question. Quand le GM l'affiche, son texte et son image apparaissent sur la TV, les champs de saisie s'ouvrent sur les téléphones et le compte à rebours démarre. Choix de réalisation, ajustable sans nouvelle décision.
6. **Normalisation et tolérance :** avant comparaison, une réponse est mise en minuscules, débarrassée de ses accents, de sa ponctuation, des espaces superflus et d'un article initial (le, la, les, l', un, une, des, the). La tolérance de Levenshtein dépend de la longueur de la réponse attendue normalisée : aucune jusqu'à 3 caractères, 1 de 4 à 7, 2 au-delà. Une question à réponse numérique (`inputMode` à `numeric`) n'a aucune tolérance. Choix de réalisation, ajustable sans nouvelle décision.

## Ordre de réalisation suggéré

1. US-E16-01.
2. US-E16-02, US-E16-03, puis US-E16-04.

## Critère de sortie (phase 5)

Voir le README de [E17](../E17-outils-de-creation-de-contenu/README.md#critère-de-sortie-phase-5). Pour cette épopée : une manche de questions ouvertes est jouée de bout en bout sur de vrais appareils (au moins un iPhone sous Safari et un Android sous Chrome), avec l'écran TV et la console GM. La saisie est confortable sur les deux téléphones (clavier adapté, champ visible au-dessus du clavier, pas de correction automatique), et le pré-classement épargne au GM la plupart des clics.
