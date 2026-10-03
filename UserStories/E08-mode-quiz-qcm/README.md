# E08 — Mode quiz QCM

**Phase :** 2. Premier mode : quiz QCM
**Objectif :** le premier mode de jeu complet. Le GM présente chaque question, ouvre les réponses, puis révèle la bonne. Les joueurs choisissent une proposition sur leur téléphone, avant la fin d'un compte à rebours, et la TV montre la question, l'avancement des réponses, puis qui a choisi quoi. Le mode valide le modèle de E07 et le format de pack de E06.

## User stories

| US | Titre | Statut | Dépend de |
|---|---|---|---|
| [US-E08-01](US-E08-01-descripteur-quiz.md) | Descripteur d'une manche de quiz | Terminée | US-E06-01, US-E07-01 |
| [US-E08-02](US-E08-02-presentation-question.md) | Présentation de la question | Terminée | US-E06-04, US-E07-02, US-E07-03, US-E08-01 |
| [US-E08-03](US-E08-03-reponses-et-compte-a-rebours.md) | Réponses ouvertes et compte à rebours | Terminée | US-E08-02 |
| [US-E08-04](US-E08-04-revelation.md) | Révélation de la bonne réponse | Terminée | US-E08-03 |
| [US-E08-05](US-E08-05-avancer-et-passer.md) | Question suivante, question passée et fin de manche | Terminée | US-E08-04 |
| [US-E08-06](US-E08-06-renvoi-des-intentions.md) | Renvoi des intentions après une coupure | Terminée | US-E08-03 |

## Décisions

Toutes les décisions qui bloquaient l'épopée sont prises. Les valeurs numériques ci-dessous sont des valeurs de départ, ajustables sans nouvelle décision.

1. **Déroulé piloté par le GM :** une question passe par quatre phases, `Presentation`, `Answering`, `Locked` et `Revealed`. Le GM ouvre les réponses, révèle la bonne réponse, puis passe à la question suivante. Seul le verrouillage est automatique : dès que tous les participants ont répondu, sinon à la fin du compte à rebours. Révisé le 2026-10-03 : le bouton « Verrouiller maintenant » de la console GM servait surtout quand tout le monde avait répondu ; ce verrouillage est devenu automatique, et le bouton a disparu avec l'intention `quiz.lockAnswers`. Révisé de nouveau le 2026-10-03 : pendant la présentation, le GM affiche la question puis chaque proposition sur la TV au fil de sa lecture (décision 12). Options écartées : un déroulé semi-automatique (ouverture et révélation automatiques) et un déroulé entièrement automatique.
2. **Temps de réponse :** une durée par défaut pour la manche (`answerSeconds`, 20 s si absente), surchargeable question par question. Bornes : de 5 à 120 s. Options écartées : une durée unique pour la manche, ou une durée obligatoire sur chaque question.
3. **Réponse définitive :** un joueur ne choisit qu'une fois, et son premier choix est le bon. Option écartée : une réponse modifiable jusqu'au verrouillage.
4. **Révélation avec les pseudos :** la TV montre, sous chaque proposition, le nombre de joueurs qui l'ont choisie et leurs pseudos, ainsi que les joueurs sans réponse. Chaque téléphone montre si son joueur a eu juste. Options écartées : une répartition anonyme, et un réglage par pack.
5. **Joueur arrivé en cours de partie :** il participe à toute question dont les réponses s'ouvrent après son inscription, et commence à 0 point (E09). La règle définitive reste à trancher en phase 6. Option écartée : répondre à la question déjà ouverte.
6. **Propositions :** de 2 à 4 par question, exactement une bonne, chacune distinguée par une lettre (A à D), une forme et une couleur, jamais par la couleur seule. L'ordre est celui du descripteur, sauf si la manche demande un mélange (`shuffleChoices`), reproductible grâce à `context.Random`.
7. **Images :** une question peut avoir une image, affichée sur la TV seulement. Les téléphones n'affichent aucun média.
8. **Mode quiz enregistré avant de jouer ses questions** (décidé pendant US-E08-01) : la vérification d'un descripteur appartient au mode (`IGameMode`), et le chargement des packs (US-E06-02) en a besoin avant que les questions soient jouées (US-E08-02). `QuizMode` est donc créé et enregistré dès US-E08-01, avec sa vérification complète et un déroulé provisoire : la manche se termine dès son démarrage. `PackProblem` et `IGameMode.Validate`, prévus par US-E06-02, sont introduits en même temps, avec les seuls codes du quiz. Option écartée : des règles du quiz dans une classe à part, que US-E06-02 aurait dû relier au chargement sans mode enregistré.
9. **Téléphone en simple pavé de réponse** (décidé pendant US-E08-02) : le téléphone n'affiche ni la question ni le texte des propositions, seulement un bouton par proposition, avec sa lettre, sa forme et sa couleur. Les joueurs lisent tout sur la TV et gardent la tête levée, comme dans une salle de jeu. La projection `Player` ne contient donc que les lettres des propositions. Option écartée : la question et les propositions recopiées sur chaque téléphone, qui gardent les joueurs le nez sur leur écran.
10. **Un type par intention, préfixé par le mode** (décidé pendant US-E08-03) : chaque intention du quiz est un type de contrat à part (`QuizSubmitAnswer`, `QuizOpenAnswers`, `QuizLockAnswers`, puis ceux de US-E08-04 et US-E08-05), dont le `type` sur le fil est celui des manches du mode, un point, puis le nom de l'intention : `quiz.submitAnswer`, `quiz.openAnswers`, `quiz.lockAnswers` (retirée depuis, voir la décision 1). Chacune ne porte que ses champs, et le client associe une intention à son mode par ce préfixe (`ModeViews` dans `shared/modeViews.ts`). Les types `quiz` provisoires de E07 (décision 3 du README de E07) disparaissent. Option écartée : un seul type d'intention par rôle et par mode, avec un champ `action` et des champs facultatifs selon l'action.
11. **`ClientSeq` dans une enveloppe** (décidé pendant US-E08-06) : `SendRoundIntent` reçoit une `PlayerIntentEnvelope` (`clientSeq`, `intent`), et les intentions des modes restent inchangées. Le mode construit l'intention, la file d'envoi de `shared/connection` la numérote et l'enveloppe ; le hub copie le numéro dans `PlayerRoundInput`. Option écartée : un champ `clientSeq` dans le type de base `PlayerRoundIntent`, à relayer par chaque intention de chaque mode, et que les vues des modes auraient dû omettre.
12. **Présentation progressive** (décidé le 2026-10-03, après l'épopée) : plutôt que d'afficher d'un coup la question et ses propositions, la TV ne montre d'abord que le numéro de la question. Le GM, qui lit tout sur sa console, affiche la question avec son image (`quiz.showQuestion`), puis chaque proposition dans l'ordre des lettres (`quiz.showChoice`, qui nomme la lettre, pour qu'un double envoi n'en affiche qu'une). Le déroulé est le même pour toutes les manches, sans réglage dans le pack : « Ouvrir les réponses » reste proposé à chaque étape et affiche d'un coup ce que la TV cache encore. La projection `Display` ne contient rien de ce que la TV n'affiche pas encore, mais donne le nombre de propositions, pour que la TV leur garde leur place. Les téléphones montrent tous leurs boutons dès le départ, et l'image s'affiche avec le texte de la question. Options écartées : un réglage par manche dans le pack, une présentation obligatoire étape par étape sans raccourci, des boutons qui apparaissent sur les téléphones au fil de la TV, et une étape distincte pour l'image.

## Tests E2E

Le serveur des tests E2E n'héberge qu'une partie, et la lancer est irréversible (US-E04-05). Les scénarios du quiz s'enchaînent donc sur une seule partie, dans le projet Playwright `launch`, avec un pack court dédié aux tests, chargé par `Packs:Directory` (US-E06-03). Les participants y incluent les joueurs des autres tests, souvent partis : seul le compte à rebours verrouille alors les réponses, et le pack le raccourcit sur les questions qui vont jusqu'à la révélation.

## Ordre de réalisation suggéré

1. US-E08-01, juste après US-E06-01 et US-E07-01.
2. US-E08-02, US-E08-03 et US-E08-04, dans cet ordre.
3. US-E08-05 et US-E08-06, en parallèle.

## Critère de sortie (phase 2)

Porté par E09 : une partie de 10 questions jouée de bout en bout par trois joueurs, et les tests de non-fuite qui passent pour chaque phase et chaque rôle.
