### US-E04-02 — Rejoindre la partie avec un pseudo

**Statut :** Terminée

**En tant que** joueur
**je veux** choisir un pseudo sur mon téléphone et rejoindre la partie
**afin d'**apparaître sur l'écran TV et de participer, sans créer de compte

**Critères d'acceptation**
- Étant donné un téléphone sans jeton qui ouvre la page joueur, quand elle se charge, alors elle affiche un champ de pseudo et un bouton « Rejoindre ».
- Étant donné un pseudo valide et libre, quand le joueur valide, alors le serveur l'inscrit, lui attribue un `PlayerId` et un jeton, et le téléphone affiche l'écran d'attente du lobby avec son pseudo (« Tu es inscrit sous le nom … »).
- Étant donné un pseudo vide, trop long (plus de 16 graphèmes), contenant des caractères de contrôle ou invisibles, quand le joueur valide, alors le serveur le refuse avec un code (`NicknameInvalid`) et le téléphone affiche un message clair sous le champ, sans perdre la saisie. Le client signale ces cas avant envoi, mais seul le serveur décide.
- Étant donné un pseudo déjà pris, à la casse et aux accents près (« Zoé » contre « zoe »), quand le joueur valide, alors le serveur le refuse avec un code (`NicknameTaken`) et le téléphone invite à en choisir un autre.
- Étant donné deux téléphones qui envoient le même pseudo au même instant, quand le serveur les traite, alors un seul est inscrit : la boucle les traite l'un après l'autre.
- Étant donné un joueur inscrit, quand on inspecte son `localStorage`, alors il contient son jeton. Le jeton n'apparaît dans aucun snapshot, ni le sien ni celui des autres.
- Étant donné la partie déjà lancée, quand un nouveau téléphone rejoint, alors il est inscrit de la même façon et reçoit le snapshot courant (décision 3 du README).
- Étant donné l'interface joueur, quand on l'utilise sur Safari iOS et Chrome Android, alors le viewport n'est pas zoomable, le champ ne provoque pas de zoom à la saisie (police d'au moins 16 px), et les zones tactiles font au moins 48 px avec `touch-action: manipulation`.
- Étant donné les tests du moteur, quand ils s'exécutent, alors ils couvrent l'inscription, chaque cas de refus et la normalisation du pseudo. Un test E2E Playwright inscrit trois joueurs dans trois contextes de navigateur.

**Comportement en cas d'erreur**
Joueur : serveur injoignable, le bouton reste désactivé et l'indicateur de reconnexion apparaît après 3 s (US-E05-02) ; la saisie est conservée. Public et GM : rien, tant que l'inscription n'a pas abouti.

**Notes techniques**
- Intention `JoinGame(nickname)`. Le hub génère le jeton (128 bits aléatoires avec `RandomNumberGenerator`, encodés en base64url) et le `PlayerId`, puis dépose l'entrée dans la file et attend la réponse de la boucle (US-E03-02). La réponse au client est soit le jeton et le `PlayerId`, soit un code de refus.
- La partie moteur est déjà livrée par US-E03-01 : intention `JoinGame` (identifiant, jeton et pseudo fournis par le hub), règles du pseudo dans `NicknameRules`, et motifs de refus `NicknameInvalid`, `NicknameTaken` et `PlayerAlreadyJoined` (inscription rejouée). Il reste à traduire ces motifs en codes de `PartyGame.Contracts` dans la réponse au client.
- L'état associe le jeton au `PlayerId`. Cette correspondance n'est jamais projetée : un jeton ne circule qu'une fois, dans la réponse à l'inscription.
- La normalisation (espaces, comparaison sans casse ni accents) est une fonction pure du moteur, partagée par l'inscription et le renommage (US-E04-04). Les graphèmes se comptent avec `StringInfo`.
- Présence : un joueur inscrit est marqué connecté. Quand sa dernière connexion se ferme, le hub dépose une entrée `PlayerConnectionLost` ; la reconnexion (US-E05-01) le marque de nouveau connecté. Cet état alimente les listes de la TV et du GM.
- Le dernier pseudo utilisé est aussi mémorisé, pour préremplir le champ si le jeton n'est plus reconnu (US-E05-01).
- Tous les textes et la traduction des codes de refus sont dans `fr.ts`.
- Réalisation : contrats `JoinRequest(nickname)` et `JoinResult(refusal, playerId, token)`, codes `JoinRefusal` : `NicknameInvalid`, `NicknameTaken`, `AlreadyJoined` (la connexion a déjà inscrit un joueur), `JoinFailed` (bug du moteur, ou connexion fermée pendant l'inscription) et `MessageInvalid`. Le hub expose `JoinGame` ; le motif `PlayerAlreadyJoined` du moteur, qui supposerait une collision d'identifiants, devient `JoinFailed`.
- Réalisation : le hub place la connexion dans le groupe du joueur avant de déposer l'inscription, pour ne manquer aucune diffusion, et l'en retire en cas de refus. L'inscription est déposée sans annulation liée à la connexion : une fois dans la file, elle peut être acceptée, et la connexion doit alors être suivie. Après acceptation, le hub envoie aussi le snapshot courant à l'appelant, comme pour une annonce.
- Réalisation : présence. `Player.IsConnected` est vrai à l'inscription. `PlayerConnections` (hub, hors de l'état) compte les connexions de chaque joueur ; `OnDisconnectedAsync` dépose `PlayerConnectionLost` quand la dernière se ferme, et le moteur marque le joueur déconnecté (refus `PlayerUnknown` et `PlayerAlreadyDisconnected`). Une connexion fermée pendant le traitement de l'inscription est rattrapée par le hub. Les projections n'exposent pas encore la présence : c'est l'objet de US-E04-03 et US-E04-04.
- Réalisation : journalisation `Information` de l'arrivée (« Player {PlayerId} joined as {Nickname} ») et de la perte de la dernière connexion ; le jeton n'est jamais journalisé (vérifié par test).
- Réalisation : côté client, `PlayerSession` (`shared/connection/playerSession.svelte.ts`) envoie l'inscription, conserve le jeton sous `partygame.player.token` et le dernier pseudo sous `partygame.player.nickname` (`localCodeStorage`), et alimente le store du snapshot `Player`. `player/nickname.ts` signale avant envoi un pseudo vide (bouton désactivé), trop long (graphèmes comptés avec `Intl.Segmenter`) ou contenant des caractères de contrôle ; les cas plus fins (caractères invisibles) sont laissés au serveur.
- Réalisation : `player/JoinForm.svelte` affiche le message sous le champ tant qu'il contient le pseudo refusé, conserve la saisie et redonne le focus ; `player/LobbyScreen.svelte` affiche « Tu es inscrit sous le nom … ». Champ en 1,25 rem (pas de zoom iOS), zones tactiles de 48 px.
- Réalisation : tant que US-E05-01 n'est pas livrée, un rechargement ramène au formulaire, prérempli avec le dernier pseudo, que le joueur précédent occupe toujours : il faut en choisir un autre. Après une reconnexion SignalR, la nouvelle connexion ne reçoit plus les snapshots du joueur.
- Réalisation : tests du moteur (`PresenceTests`, `RegistrationTests`), tests d'intégration du hub (`JoinGameTests` : inscription, refus, concurrence sur un même pseudo, message malformé, jeton absent des snapshots et des logs, présence à la déconnexion), Vitest (`playerSession.test.ts`, `nickname.test.ts`) et Playwright (`e2e/player.spec.ts`, sur iPhone et Pixel : trois joueurs dans trois contextes, pseudo pris, pseudo trop long, absence de zoom). Les tests de l'écran TV qui supposaient un serveur sans joueur s'appuient désormais sur l'invitation à rejoindre.
- Réalisation : vérifié sur un vrai téléphone, sur le serveur .NET servant le front construit : saisie du pseudo, inscription, écran du lobby.

**Hors périmètre**
- Reconnexion par jeton après une veille ou un rechargement (US-E05-01).
- Avatar, couleur ou équipe du joueur (E20 pour l'habillage, phase 6 pour les équipes).
- Changement de pseudo par le joueur lui-même.
