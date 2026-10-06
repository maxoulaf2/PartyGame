### US-E19-03 — Saut et réordonnancement des manches

**Statut :** Terminée

**En tant que** game master
**je veux** voir le programme des manches, en sauter et en changer l'ordre pendant la soirée
**afin de** adapter la soirée à l'ambiance et au temps qui reste

**Critères d'acceptation**
- Étant donné une partie lancée, quand le GM ouvre « Programme » dans la console, alors il voit toutes les manches du pack avec leur titre et leur mode : les manches jouées ou passées, la manche en cours, puis les manches à venir dans l'ordre où elles seront jouées.
- Étant donné l'entre-deux-manches ou l'introduction d'une manche, quand le GM déplace une manche à venir avec « Monter » ou « Descendre », alors la prochaine manche annoncée suit le nouvel ordre. Les manches jouées et la manche en cours ne bougent pas.
- Étant donné une manche à venir, quand le GM appuie sur « Retirer », alors elle n'est pas jouée ; « Remettre » la replace en fin de programme. Une manche retirée ne compte pas dans le total affiché (« Manche 2/3 »).
- Étant donné une manche en cours, quand le GM appuie sur « Passer la manche » et confirme, alors elle se termine comme une manche défaillante passée (US-E10-02), avec le classement intermédiaire ou final. Le bouton est disponible en permanence, et non plus seulement pour une manche défaillante.
- Étant donné toutes les manches restantes retirées, quand la manche en cours se termine, alors la partie passe au classement final.
- Étant donné deux consoles GM qui modifient le programme en même temps, quand les deux intentions arrivent, alors seule la première s'applique : chacune nomme l'ordre qu'elle remplace.
- Étant donné la TV et les téléphones, quand le programme change, alors rien ne s'affiche : les autres rôles découvrent les manches au fil de la partie.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent le réordonnancement et le retrait dans le moteur, leurs rejets (manche jouée ou en cours, ordre périmé, manche inconnue, lobby), la persistance du programme, le test de non-fuite (ni la TV ni un téléphone ne reçoivent le programme), et un scénario E2E qui change l'ordre et retire une manche.

**Comportement en cas d'erreur**
Défaut : rien côté joueurs et public. Une intention obsolète est rejetée, et la console affiche le programme à jour.

**Notes techniques**
- L'état de la partie porte le programme : la liste ordonnée des manches à venir, en indices du pack, et celle des manches retirées. `NextRound` prend la première manche du programme au lieu de la manche suivante du pack.
- Intention GM `ReorderRounds(gameId, expectedOrder, newOrder)`, `[GameMasterOnly]` : `newOrder` est une permutation des manches à venir et retirées, plus un indicateur de retrait par manche. Rejetée si `expectedOrder` ne correspond plus.
- Projection `GameMaster` seulement : le programme complet. Les projections `Display` et `Player` ne portent que le numéro de la manche en cours et le total des manches programmées.
- Le numéro d'une manche (« Manche 2/3 ») est sa position dans la partie, pas dans le pack.
- La console GM utilise des boutons « Monter » et « Descendre », utilisables au doigt sur un téléphone, plutôt qu'un glisser-déposer.
- Réalisation : moteur. `GameState.Schedule` (`RoundSchedule`) porte les manches passées (et celles d’entre elles qui ont été passées par le GM), à venir et retirées, en indices du pack ; avec la manche courante, chaque manche du pack y figure une fois. Le lancement programme toutes les manches dans l’ordre du pack ; `RoundFlow.Announce` prend la première manche à venir, et la partie se termine quand il n’en reste aucune, à la fin de la manche en cours ou à `NextRound` si le GM a tout retiré entre deux manches. `Programme` traite `ReorderRounds` dans les phases `RoundIntro`, `Round` et `BetweenRounds`, pendant une pause comprise. Rejets : `NotReorderable` (lobby, partie terminée ; `GamePending` pendant le choix d’une partie trouvée), `GameMismatch`, `ScheduleObsolete` (ordre attendu périmé), `RoundUnknown`, `RoundFixed` (manche passée ou courante), `ScheduleIncomplete` (manche manquante ou en double). Un ordre identique est accepté sans changement.
- Réalisation : projections. `RoundInfo.Number` et `Count` viennent du programme ; seule la projection `GameMaster` porte `Schedule` (`GameMasterScheduledRound` : indice, titre, mode, `ScheduledRoundStatus`), et `NextRoundTitle` suit le programme.
- Réalisation : persistance. Le programme est enregistré avec la partie ; une partie enregistrée avant lui suit l’ordre du pack (`SavedGameLoader`).
- Réalisation : hub. `ReorderRounds` (`ReorderRoundsRequest`), `[GameMasterOnly]`, ne répond rien ; un changement accepté est journalisé en `Information`.
- Réalisation : client. `gm/ScheduleControl.svelte`, repliable sous « Programme » : statut de chaque manche, « Monter », « Descendre » et « Retirer » pour les manches à venir, « Remettre » pour les manches retirées, calculés par `gm/scheduleEdit.ts`. « Passer la manche » (`SkipRoundBanner`) est proposé pour toute manche annoncée ou en cours, en alerte seulement pour une manche défaillante. Entre deux manches, sans manche à venir, le bouton devient « Classement final ».
- Réalisation : tests. `ProgrammeTests` (ordre, retrait, remise, fin de partie, pause, manche passée, rejets, retour au lobby, aller-retour JSON), scénarios et paire « order of the rounds to come » de `SnapshotsLeakTests`, `ReorderRoundsTests` côté hub (dont une partie enregistrée avant le programme), `scheduleEdit.test.ts` et `e2e/schedule.spec.ts`.

**Hors périmètre**
- Rejouer une manche déjà jouée ou passée.
- Changer de pack en cours de partie.
- Réordonner les questions à l'intérieur d'une manche.
