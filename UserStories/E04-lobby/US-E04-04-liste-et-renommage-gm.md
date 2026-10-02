### US-E04-04 — Liste des joueurs et renommage par le GM

**Statut :** Terminée

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
- Réalisation : contrats `RenamePlayerRequest(playerId, nickname)` et `RenamePlayerResult(refusal)`, codes `RenamePlayerRefusal` : `NicknameInvalid`, `NicknameTaken`, `PlayerUnknown`, `RenameFailed` (bug du moteur) et `MessageInvalid`. `GameMasterSnapshot` remplace `PlayerCount` par `Players`, une liste de `GameMasterPlayer(Id, Nickname, IsConnected)` dans l'ordre d'arrivée ; la console compte la liste. La projection ne porte que l'identifiant, jamais le jeton (test de non-fuite comparé en JSON à l'état sans jetons).
- Réalisation : le hub expose `RenamePlayer`, marqué `[GameMasterOnly]` : sans authentification, l'intention est ignorée et la réponse est vide. L'intention moteur `RenamePlayer` est traitée par `Lobby/Renaming` avec `NicknameRules`, dans toutes les phases et que le joueur soit connecté ou non. Le pseudo actuel du joueur n'est pas un doublon de lui-même : une variante de casse ou d'accents le renomme, le pseudo identique est accepté sans nouvelle version ni diffusion. Journalisation `Information` du renommage (« Player {PlayerId} renamed {Nickname} by the game master »).
- Réalisation : côté client, `GameMasterSession.rename` envoie l'intention et traduit la réponse (`renamed`, code de refus, ou `unreachable` si la connexion est perdue ou la réponse vide). `gm/LobbyConsole.svelte` affiche le nombre de joueurs inscrits et connectés, puis une liste en une colonne : pseudo, état (icône Wi-Fi, barrée si déconnecté, et libellé « Connecté » ou « Déconnecté »), bouton « Renommer » de 48 px. `gm/RenameForm.svelte` s'ouvre dans la ligne, prérempli et sélectionné, un seul à la fois ; il affiche la raison d'un refus sous le champ tant qu'il contient le pseudo refusé, conserve la saisie si la connexion tombe, et les boutons restent désactivés tant que la connexion n'est pas rétablie et le snapshot rafraîchi. Échap ou « Annuler » le ferme.
- Réalisation : la vérification avant envoi du pseudo (`nickname.ts`) passe de `player/` à `shared/`, partagée par l'inscription et le renommage. L'icône de connexion devient `shared/components/ConnectionIcon.svelte`, utilisée par la TV et le GM. Le téléphone mémorise le pseudo de chaque snapshot reçu : après un renommage, le formulaire d'inscription est prérempli avec le nouveau.
- Réalisation : un joueur déconnecté renommé voit son nouveau pseudo dès qu'il reçoit un snapshot ; la reconnexion par jeton qui le lui renverra est l'objet de US-E05-01.
- Réalisation : tests du moteur (`RenamingTests` : renommage, joueur déconnecté, partie lancée, variante de son propre pseudo, pseudo identique, pseudo pris, invalide, joueur inconnu ; `SnapshotsTests` : liste du GM, pseudo renommé dans les trois projections, non-fuite), tests d'intégration du hub (`RenamePlayerTests` : diffusion du nouveau pseudo vers la TV, le GM et le téléphone, joueur déconnecté, refus sans diffusion, joueur inconnu, pseudo identique, intention ignorée sans authentification GM ou en tant que TV, message malformé), Vitest (`gameMasterSession.test.ts`, `playerSession.test.ts`) et Playwright (`e2e/gm.spec.ts`, sur iPhone, Pixel et desktop : renommage refusé puis accepté, vérifié sur la console, la TV et le téléphone ; joueur affiché déconnecté après son départ).
- Réalisation : la vérification sur de vrais appareils (iPhone et Android) reste à faire.

**Hors périmètre**
- Exclusion d'un joueur (décision 4 du README).
- Ajustement des scores et contrôles avancés (E19).
- Réordonnancement de la liste.
