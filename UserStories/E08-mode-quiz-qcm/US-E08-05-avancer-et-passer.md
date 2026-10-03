### US-E08-05 — Question suivante, question passée et fin de manche

**Statut :** Terminée

**En tant que** game master
**je veux** passer à la question suivante quand la salle est prête, ou passer une question qui pose problème
**afin de** garder la main sur le rythme de la manche

**Critères d'acceptation**
- Étant donné une question révélée, quand le GM appuie sur « Question suivante », alors la question suivante de la manche passe en présentation sur tous les écrans.
- Étant donné la dernière question de la manche révélée, quand la console l'affiche, alors le bouton devient « Terminer la manche ». Son appui termine la manche, et la partie passe entre deux manches, ou se termine si c'était la dernière (US-E07-01).
- Étant donné une question en présentation, en réponses ouvertes ou verrouillée, quand le GM appuie sur « Passer la question » et confirme, alors la question est abandonnée : aucun point n'est attribué, les réponses reçues sont ignorées, le timer est annulé, et la manche continue avec la question suivante, ou se termine si c'était la dernière.
- Étant donné une question passée, quand la question suivante s'affiche, alors sa numérotation tient compte de la question passée (« Question 4/5 » après la 3 passée). La TV et les téléphones n'affichent aucun message au sujet de la question passée.
- Étant donné deux consoles GM, ou un renvoi après une reconnexion, quand deux intentions visent la même question (question suivante, passer la question), alors la seconde est rejetée : une question n'est jamais sautée par erreur.
- Étant donné les tests du moteur, quand ils s'exécutent, alors ils couvrent la question suivante, la fin de manche, la question passée dans chacune des trois phases, et chaque rejet : mauvaise phase, question mal ciblée, intention en double. Un test E2E enchaîne deux questions, puis en passe une.

**Comportement en cas d'erreur**
Joueurs et public : rien, l'écran suivant arrive avec le snapshot. GM : connexion perdue, la console est verrouillée (US-E05-02) ; un appui envoyé juste avant la coupure peut être renvoyé sans risque (décision 1 du README de E07).

**Notes techniques**
- Intentions GM `NextQuestion(round, question)` et `SkipQuestion(round, question)`. « Terminer la manche » est `NextQuestion` sur la dernière question : le mode déclare alors la manche terminée.
- La confirmation de « Passer la question » utilise `ConfirmDialog.svelte` (US-E04-05).
- Une question passée reste dans l'état de la manche, marquée comme telle, pour l'historique et la persistance (E11).
- Effet `CancelTimer` si la question passée avait ses réponses ouvertes.
- Réalisation : contrats. Deux nouvelles intentions GM, `QuizNextQuestion` (`quiz.nextQuestion`) et `QuizSkipQuestion` (`quiz.skipQuestion`), qui nomment la question par son numéro (décision 10 du README). Les vues ne changent pas : la question suivante est une nouvelle question en phase `Presentation`, et son numéro suffit à la numérotation.
- Réalisation : moteur. `NextQuestion` n'est acceptée qu'en phase `Revealed`, `SkipQuestion` que dans les trois phases précédentes (sinon `PhaseMismatch`), et toutes deux pour la question courante (sinon `QuestionMismatch`) : la seconde de deux intentions visant la même question est donc rejetée, puisque la manche est passée à la suivante. La question suivante est présentée comme la première, avec un nouveau tirage de l'ordre des propositions si la manche les mélange, sans réponse ni participant : ceux-ci sont fixés à l'ouverture des réponses, ce qui fait participer un joueur arrivé pendant la question précédente. Après la dernière question, la manche se termine (`IsFinished`) et reste sur elle, pour l'historique. `QuizRound.SkippedQuestions` garde la position des questions passées, dans l'ordre ; leurs réponses sont ignorées. Un timer de la question passée qui arriverait malgré son annulation est rejeté comme obsolète (`UnexpectedTimer`).
- Réalisation : client. Console GM : « Passer la question » est actif dans les trois phases avant la révélation et ouvre `ConfirmDialog` (« Passer la question 2 ? », avec un message propre à la dernière question, qui termine la manche) ; la fenêtre se ferme d'elle-même si la manche avance entre-temps, par exemple depuis une seconde console. Après la révélation, « Question suivante », ou « Terminer la manche » sur la dernière question. La TV et les téléphones n'ont pas changé.
- Réalisation : tests. Moteur : `QuizMoveOnTests` (question suivante, tirage du mélange, retardataire qui participe à la suivante, fin de manche et fin de partie, question passée dans chacune des trois phases avec ou sans `CancelTimer`, dernière question passée, questions passées successives, timer et réponse de la question passée rejetés, rejets en mauvaise phase, d'une autre question, d'une autre manche et en double, numérotation après une question passée) ; `QuizLeakTests` ajoute des scénarios de question suivante et de question passée, et des paires qui vérifient qu'une question passée ne laisse aucune trace (bonne réponse, choix de Zoé, Zoé a répondu ou non), pour tous les rôles. Hub : `QuizMoveOnTests` envoie deux fois chaque intention, vérifie que le timer d'une question passée ne change rien et qu'une réponse renvoyée en retard ne compte pas pour la suivante, puis termine la partie. E2E : le pack `soiree` compte trois questions dans sa première manche ; `gm.spec.ts` couvre « Question suivante », « Terminer la manche », la confirmation et l'annulation du passage, et le message de la dernière question ; `launch.spec.ts` enchaîne la deuxième question, la passe pendant les réponses, joue la troisième et termine la manche.

**Hors périmètre**
- Le retour à une question précédente.
- Le saut d'une manche entière, la pause et le réordonnancement (E19).
- La proposition automatique de passer une question qui échoue de façon répétée (E10).
