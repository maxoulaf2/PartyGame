### US-E12-04 — Diagnostic réseau sur place

**Statut :** Prête

**En tant que** game master arrivant dans un lieu inconnu
**je veux** vérifier en une minute que les téléphones communiquent bien avec le serveur sur le Wi-Fi du lieu
**afin de** détecter un réseau inadapté avant l'arrivée des invités, et pas en pleine partie

**Critères d'acceptation**
- Étant donné la console GM, quand elle s'affiche, alors une section « Diagnostic réseau » donne l'adresse de la page (`http://<adresse>:<port>/diagnostic/`) et son QR code. La TV ne l'affiche pas.
- Étant donné un téléphone qui ouvre `/diagnostic/`, quand la page se charge, alors un test d'environ 20 secondes démarre sans inscription : le téléphone n'apparaît ni dans le lobby ni dans les joueurs.
- Étant donné le test en cours, quand il mesure, alors il vérifie : la connexion au hub et le transport obtenu (WebSocket, ou repli), le temps d'aller-retour sur une salve (médiane, maximum, gigue), la stabilité sur 15 secondes (un aller-retour par seconde, pertes et reconnexions), le débit d'un téléchargement d'environ 1 Mo servi par le serveur, et si l'adresse du téléphone appartient au même sous-réseau que l'adresse annoncée.
- Étant donné le test terminé, quand le résultat s'affiche, alors la page donne un verdict simple, « Tout est bon », « Utilisable, avec des réserves » ou « Problème », avec pour chaque mesure en défaut un conseil en français (par exemple « Ce téléphone passe par un autre réseau : réseau invité ? »), et un bouton « Relancer le test ».
- Étant donné un test terminé, quand le résultat est envoyé au serveur, alors la console GM liste les derniers diagnostics : type d'appareil déduit du navigateur (« iPhone », « Android »…), heure, verdict et temps d'aller-retour médian.
- Étant donné des joueurs connectés pendant la partie, quand la console GM affiche la liste des joueurs, alors elle montre aussi, pour chacun et pour l'écran TV, le temps d'aller-retour récent, le transport utilisé et le nombre de reconnexions, avec un repère discret quand une valeur est mauvaise.
- Étant donné un téléphone qui n'arrive pas à ouvrir la page, quand le GM consulte [docs/installation.md](../../docs/installation.md), alors une section « Diagnostic sur place » explique quoi vérifier : même réseau Wi-Fi que le serveur, réseau invité, isolation des clients sur le point d'accès, test depuis le PC hôte avec l'adresse locale, et repli sur un partage de connexion (E22).
- Étant donné `/diagnostic` sans barre oblique finale ou dans une autre casse, quand il est ouvert, alors il est redirigé vers `/diagnostic/`, côté serveur comme dans Vite.
- Étant donné les données de diagnostic, quand les projections sont produites, alors seules la console GM et la page de diagnostic qui a fait le test les reçoivent : rien dans les projections `Player` et `Display` (test de non-fuite).
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent : Vitest (calcul des mesures et du verdict selon les seuils), intégration du hub (rapport de diagnostic, qualité de connexion par joueur dans la console GM, rien chez les joueurs ni la TV), E2E (la page passe sur iPhone et Pixel émulés et son résultat apparaît dans la console GM), et la redirection de `/diagnostic`.

**Comportement en cas d'erreur**
Page de diagnostic : si le hub est injoignable, le verdict est « Problème » avec le conseil correspondant, jamais un message technique. Joueurs et public : rien. GM : les repères discrets sur les mesures mauvaises.

**Notes techniques**
- Quatrième entrée Vite (`client/src/diagnostic/`) et route serveur, avec la redirection canonique de `client/vite/canonicalPages.ts` et du serveur. Le téléchargement de test est servi sous `/api/diagnostic/` (préfixe technique existant), généré en mémoire, sans cache.
- Les allers-retours réutilisent la lecture d'horloge du hub (`ReadClock`, US-E05-03), accessible sans annonce. Le transport est lu côté serveur (`IHttpTransportFeature`). Le sous-réseau est comparé côté serveur, à partir de l'adresse distante de la connexion et des interfaces détectées (E02).
- Qualité de connexion des joueurs : les clients rapportent périodiquement le temps d'aller-retour de leur synchronisation d'horloge, déjà mesuré toutes les 60 s ; le serveur compte lui-même les reconnexions. Ces mesures changent souvent et ne sont pas l'état de la partie : elles vont à la console GM par un message dédié, toutes les quelques secondes, sans faire avancer la version des snapshots.
- Seuils de départ, ajustables sans nouvelle décision : aller-retour médian inférieur à 50 ms bon, jusqu'à 150 ms réservé, au-delà problème ; plus de 2 % de pertes ou une reconnexion pendant le test : réservé ; transport autre que WebSocket : réservé.
- Aucune API réservée aux contextes sécurisés, aucune requête hors du serveur : la page ne teste pas l'accès à Internet. Pas de log `Information` par aller-retour.
- Mettre à jour CLAUDE.md (pages servies, section « Commandes » si un script change) et `docs/installation.md`.

**Hors périmètre**
- Le diagnostic d'horloge et le flash synchronisé (E13).
- La correction automatique du réseau, et le test de l'accès à Internet.
- La procédure de repli réseau (E22).
