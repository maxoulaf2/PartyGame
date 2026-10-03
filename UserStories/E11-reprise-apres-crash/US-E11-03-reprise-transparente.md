### US-E11-03 — Reprise transparente pour les joueurs et la TV

**Statut :** À faire

**En tant que** joueur
**je veux** retrouver la partie exactement où elle en était après un redémarrage du serveur, sans rien toucher
**afin que** le crash ne se remarque que par une brève reconnexion

**Critères d'acceptation**
- Étant donné une partie reprise par le GM (US-E11-02), quand un téléphone présente son jeton, alors la reprise de session est acceptée et il affiche l'état courant : même pseudo, même score, même question, même réponse déjà donnée.
- Étant donné la partie reprise, quand les snapshots sont diffusés, alors leur version continue celle de la partie enregistrée et leur `GameId` est inchangé : aucun client ne les ignore ni ne repart de zéro.
- Étant donné un téléphone qui avait envoyé une réponse non acquittée avant le crash, quand il la renvoie après la reprise, alors elle est acceptée si le serveur ne l'avait pas traitée, ignorée sinon : le dernier `ClientSeq` de chaque joueur fait partie de l'état enregistré.
- Étant donné une question dont les réponses étaient ouvertes, quand la partie reprend, alors le compte à rebours repart avec le temps qui restait au dernier enregistrement (décision 2 du README) : la TV et les téléphones l'affichent, et les réponses se verrouillent à la nouvelle échéance.
- Étant donné des réponses reçues avant le crash, quand la question est révélée après la reprise, alors elles rapportent le même bonus de rapidité qu'en l'absence de crash. Une réponse reçue après la reprise est notée par rapport à la nouvelle échéance.
- Étant donné une question dont l'échéance était déjà dépassée au dernier enregistrement (timer non encore traité), quand la partie reprend, alors les réponses se verrouillent aussitôt.
- Étant donné une partie reprise en présentation, réponses verrouillées, révélation, entre deux manches, au lobby ou terminée, quand elle reprend, alors chaque écran affiche l'étape où elle en était, sans timer à reprogrammer.
- Étant donné l'écran TV, quand il se reconnecte, alors il affiche l'étape courante avec ses images : les identifiants des médias sont ceux de la partie enregistrée, et leurs URL restent valides.
- Étant donné la console GM, quand le nouveau code est saisi et la partie reprise, alors elle retrouve tous ses contrôles à l'étape courante.
- Étant donné un vrai iPhone (Safari) et un vrai Android (Chrome) en pleine question, quand on tue le processus du serveur, qu'on le relance avec `scripts/start.ps1` et que le GM reprend la partie, alors les téléphones reviennent dans la question sans aucune action de leurs utilisateurs.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent : moteur (reprise du quiz dans chacune de ses phases, temps restant, bonus des réponses déjà reçues, échéance dépassée) et intégration du hub (hôte arrêté puis recréé sur le même dossier de données, clients SignalR qui reprennent par jeton, renvoi d'une intention non acquittée). Le scénario E2E est l'objet de US-E12-03.

**Comportement en cas d'erreur**
Joueurs : « Reconnexion… » pendant la coupure, puis « Retour dans la partie… » jusqu'à la décision du GM. Public : écran d'attente habillé. GM : si la reprise d'une manche échoue dans le mode, la partie reprend quand même, avec l'incident `RoundHandlerFailed` et la proposition de passer la manche (US-E10-02).

**Notes techniques**
- Une entrée interne `GameResumed(savedAt)` est la première traitée par la boucle après le chargement. Le moteur la transmet au mode de la manche en cours par un nouveau membre de `IGameMode`, `Resume(round, game, shift, context)`, qui décale ses échéances et ses horodatages du temps passé hors ligne, et retourne les timers à reprogrammer. Membre abstrait dans `GameMode<,>` : chaque mode doit décider de sa reprise. Modifier `IGameMode` reste une évolution du moteur, pas d'un mode.
- Le décalage vaut l'instant de la reprise moins l'heure de l'enregistrement. Pour le quiz : `AnswersCloseAt` et les `ReceivedAt` des réponses sont décalés d'autant, ce qui conserve les bonus déjà acquis.
- Les timers ne font pas partie de l'état : ils sont reprogrammés par les effets de la reprise, jamais relus du fichier.
- La synchronisation d'horloge est refaite à la reconnexion (US-E05-03) : le compte à rebours affiché se cale sur la nouvelle échéance.
- Mettre à jour [docs/modes/quiz.md](../../docs/modes/quiz.md) (comportement à la reprise) et le guide d'ajout d'un mode s'il existe.

**Hors périmètre**
- Le scénario E2E de redémarrage (US-E12-03).
- La reprise sans action du GM (décision 1 du README).
- La pause de la partie (E19).
