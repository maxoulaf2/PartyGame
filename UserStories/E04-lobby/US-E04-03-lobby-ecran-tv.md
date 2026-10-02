### US-E04-03 — Lobby sur l'écran TV

**Statut :** Terminée

**En tant que** public
**je veux** voir sur la TV le QR code pour rejoindre et la liste des joueurs qui arrivent
**afin de** savoir qui est prêt et donner envie aux retardataires de rejoindre

**Critères d'acceptation**
- Étant donné l'écran TV ouvert sur `/display/`, quand il se connecte, alors il s'annonce comme `Display` et affiche, à partir de la projection `Display` : le QR code et l'URL de US-E02-03, le nombre de joueurs et la liste de leurs pseudos.
- Étant donné un joueur qui rejoint, quand son inscription est acceptée, alors son pseudo apparaît sur la TV en moins d'une seconde, sans rechargement.
- Étant donné un joueur renommé par le GM, quand le renommage est accepté, alors la TV affiche le nouveau pseudo.
- Étant donné un joueur dont le téléphone est déconnecté, quand la TV l'affiche, alors son pseudo reste dans la liste, atténué et accompagné d'une icône, jamais distingué par la couleur seule.
- Étant donné 20 joueurs aux pseudos de 16 caractères, quand la TV les affiche en 1080p, alors tous sont lisibles à 3 m, sans défilement, et rien d'essentiel ne se trouve à moins de 5 % d'un bord.
- Étant donné des pseudos comportant des émojis ou des caractères spéciaux (`<script>`, `&`), quand la TV les affiche, alors ils apparaissent tels quels, sans être interprétés comme du HTML.
- Étant donné la projection `Display`, quand les tests de non-fuite s'exécutent, alors elle ne contient ni jeton, ni code GM, ni liste des interfaces réseau.
- Étant donné un test E2E Playwright, quand trois joueurs rejoignent, alors leurs pseudos apparaissent sur la TV.

**Comportement en cas d'erreur**
Public : l'écran n'est jamais vide. Serveur injoignable, il garde le dernier affichage et l'indicateur discret de US-E05-02 apparaît après 3 s ; avant le premier snapshot, il affiche l'écran d'attente neutre. Adresse inconnue : le message neutre de US-E02-03 remplace le QR code, la liste des joueurs reste affichée.

**Notes techniques**
- L'adresse annoncée rejoint la projection `Display`, comme prévu en US-E02-02 : elle fait partie de l'état de la partie, initialisée au démarrage par `AddressSelection`, pour que son changement (US-E04-06) soit diffusé comme le reste. L'endpoint `/api/join` et le polling de `joinInfo.ts` sont retirés, avec leurs tests adaptés.
- La liste suit l'ordre d'arrivée, pour que les pseudos ne sautent pas à chaque inscription.
- La mise en page s'adapte au nombre de joueurs (colonnes, taille de police) ; aucun élément n'est coupé.
- Svelte échappe le texte par défaut : ne jamais utiliser `{@html}` pour un pseudo.
- Animation d'arrivée d'un joueur : facultative et sobre ; l'habillage est l'objet de E20.
- Réalisation : `DisplaySnapshot` porte `JoinAddress` (nullable) et `Players`, une liste de `DisplayPlayer(Id, Nickname, IsConnected)` dans l'ordre d'arrivée ; `PlayerCount` en est retiré, la TV compte la liste. `GameState` porte `JoinAddress`, fixée par `GameState.Create` à partir de `AddressSelection` dans `AddGameLoop` : seule l'adresse retenue entre dans l'état, jamais les autres candidates ni les noms d'interfaces. Le contrat `JoinInfo`, l'endpoint `/api/join`, `ServerPaths.Join` et `joinInfo.ts` sont supprimés ; le préfixe `/api` reste réservé.
- Réalisation : côté client, `display/App.svelte` affiche l'écran d'attente neutre jusqu'au premier snapshot, puis `display/LobbyScreen.svelte` : à gauche l'invitation, le QR code et l'URL (ou le message neutre si l'adresse est inconnue), à droite le titre, le nombre de joueurs et `display/PlayerList.svelte`. `playerListLayout.ts` choisit colonnes et taille des pseudos selon le nombre de joueurs (1 colonne jusqu'à 6, 2 jusqu'à 20, 3 au-delà ; tailles en `vh`). Un pseudo trop large passe à la ligne au lieu d'être coupé. Joueur déconnecté : opacité réduite, icône « Wi-Fi barré » et texte masqué « (déconnecté) » pour les lecteurs d'écran. Pas d'animation d'arrivée.
- Réalisation : le renommage (US-E04-04) n'a pas encore d'intention ; la TV affichera le nouveau pseudo sans changement de son côté, puisqu'elle dérive tout du snapshot et suit les joueurs par leur identifiant.
- Réalisation : tests du moteur (`SnapshotsTests` : adresse, ordre d'arrivée, joueur déconnecté, adresse absente, non-fuite comparée en JSON), du serveur (`JoinAddressTests` : adresse retenue seule dans le snapshot, adresse absente, adresse imposée, `/api/join` en 404 ; `SnapshotBroadcastTests` : liste diffusée et joueur déconnecté), Vitest (`playerListLayout.test.ts`) et Playwright. Le serveur des tests E2E est lancé avec `Network:AdvertisedAddress=192.168.1.42`. `e2e/display.spec.ts` couvre le QR code, trois joueurs qui rejoignent (affichés en moins d'une seconde, dans l'ordre, pseudo `<b>&🎉` affiché tel quel) puis un départ (pseudo atténué avec l'icône) ; un faux hub (`e2e/fakeHub.ts`, via `routeWebSocket`) sert les cas que le serveur partagé ne peut pas produire à la demande : 20 pseudos de 16 caractères tenant dans la zone sûre de 5 % sans défilement, en 30 px au moins, adresse inconnue, serveur injoignable.

**Hors périmètre**
- Bouton « Démarrer » de déblocage audio (E14).
- Jingles, thème et animations travaillées (E20).
- Plus de 20 joueurs : la mise en page reste correcte mais n'est pas optimisée.
