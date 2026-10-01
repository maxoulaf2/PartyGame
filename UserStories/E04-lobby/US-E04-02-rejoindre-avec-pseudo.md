### US-E04-02 — Rejoindre la partie avec un pseudo

**Statut :** À faire

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

**Hors périmètre**
- Reconnexion par jeton après une veille ou un rechargement (US-E05-01).
- Avatar, couleur ou équipe du joueur (E20 pour l'habillage, phase 6 pour les équipes).
- Changement de pseudo par le joueur lui-même.
