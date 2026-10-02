### US-E06-04 — Médias du pack servis sur le réseau local

**Statut :** À faire

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

**Hors périmètre**
- L'affichage de l'image d'une question (US-E08-02).
- La lecture audio, son préchargement et sa synchronisation (E14).
- Le signalement au GM d'un média illisible (E10, E14).
