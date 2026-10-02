### US-E06-04 — Médias du pack servis sur le réseau local

**Statut :** Terminée

**En tant que** public
**je veux** que les images d'un pack s'affichent sur la TV sans délai et sans rien dévoiler à l'avance
**afin de** profiter des questions illustrées sans que personne puisse deviner une réponse par le nom d'un fichier

**Critères d'acceptation**
- Étant donné le pack sélectionné, quand un client demande un de ses médias sous `/media/<identifiant>`, alors le serveur le sert avec le bon `Content-Type`.
- Étant donné une requête avec un en-tête `Range`, quand elle porte sur un média, alors le serveur répond `206 Partial Content` avec la plage demandée.
- Étant donné l'identifiant d'un média, quand on l'observe dans une projection ou dans une URL, alors il ne révèle ni le nom du fichier, ni son dossier, ni sa position dans le pack.
- Étant donné un identifiant inconnu, un média d'un autre pack que celui sélectionné, ou un chemin de fichier à la place de l'identifiant, quand il est demandé, alors le serveur répond `404`, sans lister aucun contenu.
- Étant donné la projection `Display`, quand elle référence un média, alors elle ne contient que son URL `/media/<identifiant>`, jamais le chemin écrit dans le descripteur.
- Étant donné les tests d'intégration du serveur, quand ils s'exécutent, alors ils couvrent la réponse complète, la réponse partielle, le `404` d'un identifiant inconnu ou d'un autre pack, et l'absence de nom de fichier dans les identifiants et les projections.

**Comportement en cas d'erreur**
Public : une image qui ne se charge pas (fichier supprimé pendant la partie) laisse la question affichée sans image, sans message. Le signalement au GM arrive en E10 et E14. Joueurs : rien, les téléphones n'affichent pas les médias. GM : rien en phase 2.

**Notes techniques**
- Le préfixe `/media` est déjà réservé dans `ServerPaths`.
- Les identifiants sont tirés au hasard par le serveur lors de la copie du descripteur dans l'état, au lancement (US-E06-03). La correspondance entre identifiant et fichier fait partie de l'état, pour survivre à une reprise après crash (E11), et n'apparaît dans aucune projection.
- Réponse par `Results.File(..., enableRangeProcessing: true)` ou équivalent. Un en-tête de cache longue durée est possible, puisqu'un identifiant désigne toujours le même fichier pendant une partie.
- Le chemin du fichier servi provient uniquement de la correspondance de l'état, jamais de l'URL : aucun parcours de dossier n'est possible.
- Cette US prépare l'audio sur l'écran TV (E14), qui réutilise le même service.
- Réalisation : moteur. `PackMedia` (dans `GameState.Media`) associe chaque identifiant (`MediaId`, 128 bits aléatoires en base64url, 22 caractères) au chemin écrit dans le descripteur. `Launch` le tire avec `context.Random` au lancement, une fois pour toute la partie : un identifiant sert le même fichier jusqu'au bout, et une partie rejouée retrouve les mêmes. La liste des médias d'un pack (`CatalogPack.Media`, chaque chemin une fois) vient du chargement (`LoadedPack.Media`, déjà relevée par `DescriptorReader`) : le moteur n'a pas à connaître les propriétés `MediaPath` de chaque mode. Un mode obtient l'URL d'un média par `game.Media.UrlOf(chemin)`, la seule référence qu'une projection puisse contenir ; un chemin inconnu est un bug (exception).
- Réalisation : serveur. `PackMediaFiles` sert `GET` et `HEAD` sur `/media/{id}` (`MapPackMedia`) : le fichier vient de l'état seul (dossier des packs, pack joué, chemin du média), avec une vérification supplémentaire qu'il reste dans le dossier du pack. `Results.File` avec `enableRangeProcessing` et la date de modification, `Content-Type` par `FileExtensionContentTypeProvider`, `Cache-Control: private, max-age=86400, immutable`. Un identifiant inconnu, donc tout identifiant avant le lancement, ou un chemin à la place de l'identifiant donne un `404` sans contenu. Un fichier supprimé pendant la partie donne aussi un `404`, avec un log `Warning`. `ServerPaths.Media` reprend `PackMedia.UrlPrefix`.
- Réalisation : tests. Moteur : `PackMediaTests` (identifiants distincts, sûrs dans une URL, sans rien du nom du fichier, reproductibles), `LaunchTests` (identifiants du seul pack joué, aucun si le lancement est refusé, conservés d'une manche à l'autre), `GameStateTests` (persistance), et dans `SnapshotsTests` l'URL d'une image dans la vue `Display` du mode de test et l'absence de tout chemin de média dans les projections des trois rôles, à chaque phase. Contenu : la liste des médias d'un pack valide, vide pour un pack invalide. Serveur : `Packs/PackMediaTests` (réponse complète et `Content-Type` en PNG, WebP et JPEG, réponse partielle `206`, `404` d'un identifiant inconnu, d'un chemin, d'un autre pack, d'une autre partie, avant le lancement et d'un fichier supprimé, et aucun chemin dans les identifiants ni dans les snapshots).
- Limite connue, à trancher si besoin : les identifiants viennent du générateur du moteur, dont la graine (`Random.Shared.Next()`) ne fait que 32 bits. Ils ne révèlent rien à un joueur curieux, mais quelqu'un qui retrouverait la graine par force brute, à partir des identifiants de manche visibles dans les snapshots, pourrait prédire l'ordre de tirage. Option possible : tirer les identifiants des médias par `RandomNumberGenerator` côté serveur, et les passer au moteur par le contexte.

**Hors périmètre**
- L'affichage de l'image d'une question (US-E08-02).
- La lecture audio, son préchargement et sa synchronisation (E14).
- Le signalement au GM d'un média illisible (E10, E14).
