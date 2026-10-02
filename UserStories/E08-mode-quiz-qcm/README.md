# E08 — Mode quiz QCM

**Phase :** 2. Premier mode : quiz QCM
**Objectif :** le premier mode de jeu complet. Le GM présente chaque question, ouvre les réponses, puis révèle la bonne. Les joueurs choisissent une proposition sur leur téléphone, avant la fin d'un compte à rebours, et la TV montre la question, l'avancement des réponses, puis qui a choisi quoi. Le mode valide le modèle de E07 et le format de pack de E06.

## User stories

| US | Titre | Statut | Dépend de |
|---|---|---|---|
| [US-E08-01](US-E08-01-descripteur-quiz.md) | Descripteur d'une manche de quiz | À faire | US-E06-01, US-E07-01 |
| [US-E08-02](US-E08-02-presentation-question.md) | Présentation de la question | À faire | US-E06-04, US-E07-02, US-E07-03, US-E08-01 |
| [US-E08-03](US-E08-03-reponses-et-compte-a-rebours.md) | Réponses ouvertes et compte à rebours | À faire | US-E08-02 |
| [US-E08-04](US-E08-04-revelation.md) | Révélation de la bonne réponse | À faire | US-E08-03 |
| [US-E08-05](US-E08-05-avancer-et-passer.md) | Question suivante, question passée et fin de manche | À faire | US-E08-04 |
| [US-E08-06](US-E08-06-renvoi-des-intentions.md) | Renvoi des intentions après une coupure | À faire | US-E08-03 |

## Décisions

Toutes les décisions qui bloquaient l'épopée sont prises. Les valeurs numériques ci-dessous sont des valeurs de départ, ajustables sans nouvelle décision.

1. **Déroulé piloté par le GM :** une question passe par quatre phases, `Presentation`, `Answering`, `Locked` et `Revealed`. Le GM ouvre les réponses, révèle la bonne réponse, puis passe à la question suivante. Seul le verrouillage est automatique, à la fin du compte à rebours ; le GM peut aussi verrouiller plus tôt, par exemple quand tout le monde a répondu. Options écartées : un déroulé semi-automatique (ouverture et révélation automatiques) et un déroulé entièrement automatique.
2. **Temps de réponse :** une durée par défaut pour la manche (`answerSeconds`, 20 s si absente), surchargeable question par question. Bornes : de 5 à 120 s. Options écartées : une durée unique pour la manche, ou une durée obligatoire sur chaque question.
3. **Réponse définitive :** un joueur ne choisit qu'une fois, et son premier choix est le bon. Option écartée : une réponse modifiable jusqu'au verrouillage.
4. **Révélation avec les pseudos :** la TV montre, sous chaque proposition, le nombre de joueurs qui l'ont choisie et leurs pseudos, ainsi que les joueurs sans réponse. Chaque téléphone montre si son joueur a eu juste. Options écartées : une répartition anonyme, et un réglage par pack.
5. **Joueur arrivé en cours de partie :** il participe à toute question dont les réponses s'ouvrent après son inscription, et commence à 0 point (E09). La règle définitive reste à trancher en phase 6. Option écartée : répondre à la question déjà ouverte.
6. **Propositions :** de 2 à 4 par question, exactement une bonne, chacune distinguée par une lettre (A à D), une forme et une couleur, jamais par la couleur seule. L'ordre est celui du descripteur, sauf si la manche demande un mélange (`shuffleChoices`), reproductible grâce à `context.Random`.
7. **Images :** une question peut avoir une image, affichée sur la TV seulement. Les téléphones n'affichent aucun média.

## Tests E2E

Le serveur des tests E2E n'héberge qu'une partie, et la lancer est irréversible (US-E04-05). Les scénarios du quiz s'enchaînent donc sur une seule partie, dans le projet Playwright `launch`, avec un pack court dédié aux tests, chargé par `Packs:Directory` (US-E06-03).

## Ordre de réalisation suggéré

1. US-E08-01, juste après US-E06-01 et US-E07-01.
2. US-E08-02, US-E08-03 et US-E08-04, dans cet ordre.
3. US-E08-05 et US-E08-06, en parallèle.

## Critère de sortie (phase 2)

Porté par E09 : une partie de 10 questions jouée de bout en bout par trois joueurs, et les tests de non-fuite qui passent pour chaque phase et chaque rôle.
