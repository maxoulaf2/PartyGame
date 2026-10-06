### US-E19-01 — Pause et reprise de la partie

**Statut :** À faire

**En tant que** game master
**je veux** mettre la partie en pause et la reprendre là où elle en était
**afin de** gérer un imprévu (pizzas, téléphone qui sonne, dispute sur une réponse) sans que personne perde de points

**Critères d'acceptation**
- Étant donné une partie lancée, hors du lobby et de la fin de partie, quand le GM appuie sur « Pause », alors la TV affiche « Pause » par-dessus l'écran courant, les téléphones affichent « Pause » à la place de tout élément interactif, et la console affiche « Reprendre ».
- Étant donné une question avec un compte à rebours de 30 s mise en pause à 12 s de la fin, quand le GM reprend 5 minutes plus tard, alors le compte à rebours repart de 12 s sur la TV et les téléphones, et l'échéance du serveur est décalée d'autant.
- Étant donné un extrait de blind test en cours de lecture, quand la partie est mise en pause, alors la TV coupe le son ; à la reprise, l'extrait repart à la position où il s'était arrêté, sans dépasser sa fin.
- Étant donné la pause, quand un joueur répond ou buzze (intention déjà partie avant que son écran affiche la pause), alors l'intention est refusée et son téléphone affiche ce que dit le snapshot, sans message.
- Étant donné la pause, quand le GM tente une intention de manche (révéler, question suivante, juger), alors elle est refusée ; seuls « Reprendre », le retour au lobby, le renommage et l'ajustement des scores restent possibles.
- Étant donné la pause, quand un téléphone s'inscrit ou se reconnecte, alors il affiche « Pause ».
- Étant donné le serveur arrêté pendant la pause, quand la partie est reprise après le redémarrage (E11), alors elle est toujours en pause, avec les mêmes temps restants.
- Étant donné deux consoles GM ou un double appui, quand « Pause » ou « Reprendre » arrive deux fois, alors la seconde intention est rejetée comme obsolète.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent dans le moteur la pause et la reprise dans chaque phase de chaque mode (échéances décalées, timers annulés puis replanifiés), les intentions refusées pendant la pause, la persistance de la pause, les projections avec leur test de non-fuite, et un scénario E2E de pause pendant une question de quiz.

**Comportement en cas d'erreur**
Défaut : rien côté joueurs et public. Une intention obsolète est rejetée sans effet.

**Notes techniques**
- La pause est un état de la partie (`GameState.PausedAt`, heure serveur du début de la pause), pas une phase : la phase et l'état du mode restent intacts, ce qui rend la reprise triviale.
- À la pause, l'effet `CancelRoundTimers` annule les timers de la manche. À la reprise, le moteur décale les échéances du mode de la durée de la pause et replanifie ses timers, par le même chemin que la reprise après un redémarrage (`GameResumed`, US-E11-03), qui rattrape déjà le temps passé hors ligne.
- Intentions GM `PauseGame(gameId)` et `ResumeGame(gameId)`, `[GameMasterOnly]` ; elles nomment la partie et sont rejetées si la partie est déjà dans l'état demandé.
- Audio : la projection `Display` ne décrit aucune lecture pendant la pause ; à la reprise, l'`AudioPlayback` porte la position atteinte et un nouvel instant de déclenchement.
- Le buzzer reste fermé pendant la pause : un buzz horodaté avant la pause mais reçu après est refusé.

**Hors périmètre**
- Une pause automatique quand la TV se déconnecte.
- Un écran de pause personnalisé (classement, message du GM).
