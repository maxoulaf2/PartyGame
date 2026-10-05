### US-E14-01 — Déverrouillage de l'audio sur l'écran TV

**Statut :** À faire

**En tant que** game master
**je veux** que la TV soit autorisée à jouer du son avant la première manche, et savoir si elle ne l'est pas
**afin qu'** aucun extrait ne reste muet à cause de la politique de lecture automatique du navigateur

**Critères d'acceptation**
- Étant donné l'écran TV ouvert dans un navigateur de PC, quand la page s'affiche, alors elle propose un bouton « Démarrer », bien visible au-dessus du QR code. Son clic débloque l'audio (lecture d'un son silencieux embarqué) puis disparaît.
- Étant donné l'écran TV en mode kiosque Chromium lancé avec `--autoplay-policy=no-user-gesture-required`, quand la page s'affiche, alors elle détecte que l'audio est déjà autorisé et n'affiche pas le bouton.
- Étant donné la TV rechargée en cours de partie, quand l'audio n'est plus autorisé, alors le bouton « Démarrer » réapparaît par-dessus l'écran courant, sans le masquer entièrement.
- Étant donné la console GM, quand l'écran TV connecté n'a pas l'audio débloqué, alors elle affiche un avertissement discret « Son de la TV non activé », qui disparaît dès qu'il l'est.
- Étant donné les tests, quand ils s'exécutent, alors Playwright vérifie le bouton et sa disparition, et le test d'intégration du hub l'avertissement côté GM.

**Comportement en cas d'erreur**
Public : si le déblocage échoue, le bouton reste affiché, sans message technique. GM : l'avertissement reste affiché.

**Notes techniques**
- La TV remonte l'état de l'audio avec sa qualité de connexion (`ReportConnectionQuality`, US-E12-04) ou une méthode dédiée : hors de l'état de jeu, comme la qualité réseau, et diffusé au seul groupe `gm`.
- Le son silencieux est un fichier embarqué dans le build (aucune requête externe).
- Mettre à jour `docs/installation.md` (mode kiosque et flag).

**Hors périmètre**
- Le réglage du volume depuis la console GM (E19).
