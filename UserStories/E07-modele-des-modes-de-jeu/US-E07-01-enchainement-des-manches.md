### US-E07-01 — Manches du pack enchaînées par le moteur

**Statut :** Terminée

**En tant que** game master
**je veux** que le lancement démarre la première manche du pack choisi, puis que la partie enchaîne ses manches jusqu'à la fin
**afin de** dérouler toute la soirée depuis ma console

**Critères d'acceptation**
- Étant donné un pack dont l'état contient les manches, quand le GM lance la partie, alors la première manche démarre, jouée par le mode que désigne son `type`, et chaque rôle reçoit la vue de la manche produite par ce mode, avec le numéro de la manche, le nombre total de manches et son titre.
- Étant donné une manche en cours, quand un joueur ou le GM envoie une intention propre à la manche, alors le moteur la transmet au mode de la manche en cours, avec l'état de la manche. Hors d'une manche, elle est rejetée sans effet.
- Étant donné une manche que son mode déclare terminée, quand ce n'est pas la dernière, alors la partie passe entre deux manches. Quand c'est la dernière, la partie est terminée.
- Étant donné la partie entre deux manches, quand le GM demande la manche suivante en nommant la manche qui vient de finir, alors la manche suivante démarre. Une seconde demande pour la même manche (double appui, deuxième console, renvoi après reconnexion) est rejetée sans effet (décision 1 du README).
- Étant donné un timer programmé par une manche, quand il échoit après la fin de cette manche, alors il est rejeté sans effet : chaque timer porte l'identifiant de sa manche.
- Étant donné un joueur qui rejoint pendant une manche, quand il est inscrit, alors il reçoit la vue de la manche en cours. Sa participation est décidée par le mode (US-E08-02).
- Étant donné la phase provisoire `Started`, quand cette US est réalisée, alors elle disparaît au profit des phases `Lobby`, `Round` (manche en cours), `BetweenRounds` et `Finished`. Les écrans provisoires de E04 sont remplacés par ceux de US-E07-02.
- Étant donné les tests du moteur, quand ils s'exécutent, alors un mode factice, enregistré pour le test, couvre : le lancement vers la première manche, le passage entre deux manches, la manche suivante, la fin de partie après la dernière manche, le rejet d'une intention hors manche, le rejet d'une manche suivante en double ou mal ciblée, et le rejet d'un timer d'une manche terminée.
- Étant donné l'ajout du mode quiz (E08), quand il est réalisé, alors il ne modifie ni `GameEngine`, ni `GameLoop`, ni `GameHub`, en dehors de l'enregistrement du mode dans `AddGameModes()` et des attributs `[JsonDerivedType]` de `PartyGame.Contracts`.

**Comportement en cas d'erreur**
Défaut : rien côté joueurs et public. Une exception levée par un mode est rattrapée par `GameLoop`, qui conserve l'état précédent ; le retour à l'état précédent avec incident GM et la proposition de passer la manche sont l'objet de E10. GM : une demande de manche suivante refusée comme obsolète ne produit aucun message, le snapshot montre déjà la manche en cours.

**Notes techniques**
- Interface `IGameMode` dans `PartyGame.Engine`, indicative :
  - le type d'activité qu'il joue ;
  - la vérification de la cohérence d'un descripteur, qui retourne des `PackProblem` (utilisée par US-E06-02) ;
  - le démarrage d'une manche à partir de son descripteur, qui produit l'état initial de la manche et ses effets ;
  - le traitement d'une entrée de la manche, sur le modèle de `Handle` : nouvel état de la manche, effets, motif de rejet, fin de manche, points attribués (E09) ;
  - la projection de l'état de la manche pour chaque rôle, et pour chaque joueur.
- Enregistrement explicite dans `AddGameModes()`, sans scan réflexif (conventions).
- L'état d'une manche est un `record` immuable propre au mode, dérivé d'une base commune, sérialisable pour la persistance (E11). Le contexte (`context.Now`, `context.Random`) est fourni au mode comme au moteur.
- Contrats : les vues de manche sont des bases polymorphes, une par rôle (`PlayerRoundView`, `DisplayRoundView`, `GameMasterRoundView`), présentes dans les snapshots pendant une manche. Les intentions de manche sont deux bases polymorphes, une pour les joueurs et une pour le GM. Le hub expose deux méthodes génériques pour les recevoir, celle du GM marquée `[GameMasterOnly]` : un mode n'ajoute aucune méthode au hub.
- Intention GM générique `NextRound(afterRound)`, refusée si `afterRound` n'est pas la manche qui vient de finir.
- Identifiant typé `RoundId` pour les manches. Les timers de manche le portent, et le moteur rejette ceux d'une manche terminée avant de solliciter le mode.
- Tant que US-E06-03 n'est pas réalisée, l'état peut recevoir ses manches dans les tests seulement : la démonstration sur l'application réelle passe par le choix du pack.
- Réalisation : `IGameMode` (`PartyGame.Engine.Modes`) expose `DescriptorType`, `Start`, `Handle` et les trois projections ; la base typée `GameMode<TDescriptor, TState>` évite tout transtypage dans les modes. Le registre `GameModes` retrouve un mode par le type CLR du descripteur et refuse deux modes pour le même type. `GameEngine` et `Snapshots` (devenue une classe d'instance) le reçoivent ; côté serveur, `AddGameModes()` (`PartyGame.Server/Games`) est le point d'enregistrement, encore vide.
- Réalisation : la vérification de cohérence d'un descripteur n'est pas encore dans `IGameMode` : elle arrive avec `PackProblem`, dont la forme (codes, paramètres) relève de US-E06-02.
- Réalisation : `GameState` gagne `Rounds` (descripteurs du pack) et `CurrentRound` (`PlayedRound` : `RoundId`, index, état propre au mode), conservé entre deux manches et en fin de partie pour que le GM nomme la manche qui vient de finir. `RoundId` est un `Guid` tiré de `context.Random` : une partie rejouée avec la même graine obtient les mêmes identifiants. Les phases sont `Lobby`, `Round`, `BetweenRounds` et `Finished`, côté moteur comme dans les contrats.
- Réalisation : le lancement refuse un pack dont une activité n'a pas de mode enregistré (`GameModeMissing`), pour qu'aucune manche ne puisse échouer à démarrer en cours de partie. Sans manche (aucun pack choisi tant que US-E06-03 n'est pas réalisée), le lancement termine aussitôt la partie : l'application réelle passe donc du lobby à `Finished`, et les écrans provisoires de E04 affichent « partie lancée » pour toute phase hors lobby.
- Réalisation : les intentions de manche (`PlayerRoundIntent`, `GameMasterRoundIntent`) portent toutes le `RoundId` visé ; le moteur rejette sans effet celles d'une autre manche (`RoundMismatch`) ou reçues hors manche (`NotInRound`) avant de solliciter le mode. `NextRound(afterRound)` est rejetée hors de l'entre-deux-manches (`NotBetweenRounds`, ce qui couvre la seconde demande) ou si elle nomme une autre manche (`RoundMismatch`). Le moteur marque chaque `ScheduleTimer` d'un mode avec le `RoundId`, que `TimerScheduler` recopie dans `TimerElapsed` ; un timer d'une autre manche, ou sans manche, est rejeté (`UnexpectedTimer`).
- Réalisation : les snapshots des trois rôles portent `round` (`RoundInfo` : identifiant, numéro, nombre de manches, titre ; présent en manche, entre deux manches et en fin de partie) et `roundView`, la vue du mode, présente seulement pendant une manche.
- Réalisation : System.Text.Json et `PartyGame.TypeGen` refusent une base polymorphe sans type dérivé. Les cinq bases déclarent donc dès maintenant des types `quiz` provisoires et vides dans `PartyGame.Contracts.Quiz` (`QuizPlayerView`, `QuizDisplayView`, `QuizGameMasterView`, `QuizPlayerIntent`, `QuizGameMasterIntent`), comme `QuizRoundDescriptor` en US-E06-01 (décision 3 du README). E08 les complète ou les remplace.
- Réalisation : le hub expose `SendRoundIntent` (connexion identifiée comme joueur, sinon `Warning`), `SendGameMasterRoundIntent` et `NextRound` (`[GameMasterOnly]`). Ils ne répondent rien : l'appel se termine une fois l'intention traitée par la boucle, et les snapshots montrent le résultat. `ContractJsonOptions` active `AllowOutOfOrderMetadataProperties`, pour que `type` puisse figurer n'importe où dans un message ; `HubMessage.TryRead` traite aussi un message polymorphe sans `type` (`NotSupportedException`) comme malformé, au lieu de le laisser remonter en `Error`.
- Réalisation : `RoundProgressLog`, écouteur de la boucle, journalise en `Information` le début et la fin de chaque manche et la fin de la partie, quelle qu'en soit la cause (intention, timer, fin décidée par le mode).
- Réalisation : tests du moteur avec `FakeMode` (descripteur, état, intentions et vues propres aux tests ; `FakeJson` les rend sérialisables pour les tests de non-fuite) : `RoundFlowTests`, `GameModesTests`, `LaunchTests` et `SnapshotsTests`, dont les tests de non-fuite couvrent désormais les quatre phases. Côté serveur, `RoundsTests` joue deux manches de bout en bout par de vrais clients SignalR avec `TestQuizMode`, enregistré par `ConfigureTestServices` sur les types `quiz` provisoires, et `TimerSchedulerTests` vérifie le report du `RoundId`.
- Réalisation : aucune console GM ne permet encore de demander la manche suivante : l'écran entre deux manches est l'objet de US-E07-02, et aucune partie réelle n'a de manche avant US-E06-03.

**Hors périmètre**
- Les vues client des modes (US-E07-02).
- Les scores et les classements (E09).
- L'écran d'introduction des manches (E18).
- La pause, le saut et le réordonnancement des manches (E19).
