### US-E18-02 — Classements animés et podium révélé marche par marche

**Statut :** Terminée

**En tant que** public
**je veux** voir qui a gagné ou perdu des places entre deux manches, puis découvrir le podium marche par marche
**afin de** vivre les retournements de situation et garder le suspense jusqu'au bout

**Critères d'acceptation**
- Étant donné l'entre-deux-manches, quand la TV affiche le classement, alors chaque joueur porte son évolution depuis le classement précédent (« ▲ 2 », « ▼ 1 », « = »), marquée par une flèche et un nombre, jamais par la couleur seule. Après la première manche, aucune évolution n'est affichée.
- Étant donné le classement entre deux manches, quand il apparaît, alors les lignes se placent d'abord dans l'ordre précédent puis glissent vers leur nouveau rang, en moins de 2 s.
- Étant donné la fin de la partie, quand la TV affiche le classement final, alors elle révèle d'abord le reste du classement, puis la 3e marche, la 2e, et enfin la 1re, à intervalles réguliers, la révélation complète durant moins de 10 s.
- Étant donné la révélation du podium en cours, quand un téléphone affiche l'écran de fin, alors il n'indique son rang qu'une fois sa marche révélée sur la TV, pour ne pas gâcher la surprise.
- Étant donné une TV rechargée ou reconnectée après la fin de la séquence, quand elle reçoit le snapshot, alors elle affiche le classement final complet, sans rejouer l'animation.
- Étant donné un navigateur réglé sur `prefers-reduced-motion`, quand les classements s'affichent, alors ils apparaissent directement à leur place finale, la révélation du podium gardant ses étapes.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent le calcul de l'évolution des rangs dans le moteur (premier classement, joueur arrivé en cours de partie, égalités), la projection avec son test de non-fuite, la séquence du podium en Vitest (horloge simulée) et un scénario E2E sur la TV et un téléphone.

**Comportement en cas d'erreur**
Défaut : rien côté joueurs et public. Si l'animation échoue, la vue protégée affiche le classement sans animation au snapshot suivant.

**Notes techniques**
- Le moteur mémorise le rang de chaque joueur au classement précédent ; `RankedPlayer` reçoit le rang précédent (`null` après la première manche ou pour un joueur arrivé depuis). Le client ne calcule aucun rang.
- La séquence du podium part de l'instant de fin de partie, en heure serveur, porté par le snapshot : la TV et les téléphones restent alignés grâce à la synchronisation d'horloge, et une TV rechargée sait où en est la séquence.
- Animations en CSS (`transform`, `opacity`) uniquement, pour rester fluides sur un Raspberry Pi ; aucune bibliothèque d'animation.
- Réalisation : moteur. À l'annonce de chaque manche (`RoundFlow.Announce`), le rang de chaque joueur est mémorisé dans `Player.PreviousRank` : aucun avant la première manche, aucun pour un joueur arrivé depuis. La fin de la dernière manche, par son mode ou par `SkipRound`, enregistre `GameState.FinishedAt` (heure du contexte).
- Réalisation : contrats. `RankedPlayer.PreviousRank` ; `DisplaySnapshot.FinishedAt` et `PlayerSnapshot.FinishedAt` (ms depuis l'epoch Unix, heure serveur), présents seulement une fois la partie terminée, nuls pour une partie enregistrée avant.
- Réalisation : client. `display/RankingScreen.svelte` affiche « ▲ 2 », « ▼ 1 » ou « = » avec une phrase pour les lecteurs d'écran (`fr.game.rankMove`, `rankMoveLabel`) et fait glisser les lignes depuis l'ordre précédent (`display/rankMoves.ts`, `Element.animate` sur `transform`, aucune animation sous `prefers-reduced-motion`). La séquence du podium est dans `shared/podium.ts` (`revealDelay`, `isRevealed`, `untilNextReveal`) : le reste à la fin, puis les marches 3, 2 et 1 toutes les 2,5 s. `display/FinalRankingScreen.svelte` masque chaque marche jusqu'à sa révélation, à sa place ; `player/FinalScreen.svelte` affiche « Le classement se dévoile sur l'écran… » jusqu'à celle du rang du joueur.
- Réalisation : tests. `RankingSnapshotsTests` (premier classement, classement suivant avec égalités, joueur arrivé depuis, instant de fin, y compris après un saut), paire de non-fuite « previous rank of another player », `podium.test.ts` et `rankMoves.test.ts` en Vitest, `e2e/display.spec.ts` (évolutions) et `e2e/podium.spec.ts` (révélation sur la TV et un téléphone, TV rechargée).

**Hors périmètre**
- Un roulement de tambour ou une fanfare (US-E20-02).
- Une révélation du podium pilotée par le GM.
