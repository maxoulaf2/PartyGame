### US-E04-04 — Liste des joueurs et renommage par le GM

**Statut :** À faire

**En tant que** game master
**je veux** voir la liste des joueurs avec leur état de connexion et pouvoir corriger un pseudo
**afin de** vérifier que tout le monde est là avant de lancer, et de remplacer un pseudo déplacé ou illisible

**Critères d'acceptation**
- Étant donné le GM authentifié, quand il est dans le lobby, alors il voit chaque joueur avec son pseudo et son état (connecté ou déconnecté, distingués par une icône et un libellé), ainsi que le nombre total de joueurs et de joueurs connectés.
- Étant donné un joueur de la liste, quand le GM choisit « Renommer », saisit un pseudo valide et libre puis valide, alors le pseudo change sur la TV, dans la liste du GM et sur le téléphone du joueur.
- Étant donné un nouveau pseudo invalide ou déjà pris, quand le GM valide, alors le renommage est refusé avec les mêmes règles qu'à l'inscription (US-E04-02), et l'interface GM affiche la raison sous le champ.
- Étant donné un joueur déconnecté, quand le GM le renomme, alors le renommage est accepté, et le joueur voit son nouveau pseudo à sa reconnexion.
- Étant donné un GM qui renomme un joueur avec son pseudo actuel (ou une variante de casse de celui-ci), quand il valide, alors le changement est accepté sans être considéré comme un doublon de lui-même.
- Étant donné un renommage visant un `PlayerId` inconnu, quand le serveur le reçoit, alors il est rejeté (même instance d'état, log `Debug`).
- Étant donné la projection `GameMaster`, quand on l'inspecte, alors elle contient le `PlayerId` de chaque joueur, mais jamais son jeton.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent chaque transition et chaque refus du renommage, et un test E2E renomme un joueur et vérifie le résultat sur les trois interfaces.

**Comportement en cas d'erreur**
GM : connexion perdue, les actions sont désactivées et l'indicateur de reconnexion apparaît (US-E05-02) ; une saisie en cours est conservée. Joueur concerné : il voit simplement son nouveau pseudo, sans message. Public : rien.

**Notes techniques**
- Intention GM `RenamePlayer(playerId, nickname)`, refusée sans authentification GM (US-E03-04).
- La validation et la normalisation sont celles de US-E04-02.
- L'interface GM est utilisable sur téléphone : liste en une colonne, zones tactiles d'au moins 48 px.
- Pas d'exclusion de joueur (décision 4 du README).

**Hors périmètre**
- Exclusion d'un joueur (décision 4 du README).
- Ajustement des scores et contrôles avancés (E19).
- Réordonnancement de la liste.
