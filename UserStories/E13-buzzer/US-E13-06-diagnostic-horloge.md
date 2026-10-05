### US-E13-06 — Diagnostic d'horloge et flash synchronisé

**Statut :** À faire

**En tant que** game master
**je veux** vérifier que les horloges des téléphones sont bien alignées sur celle du serveur
**afin de** garantir un départage juste avant de lancer un blind test, et de mesurer la dispersion des horloges

**Critères d'acceptation**
- Étant donné la page `/diagnostic/` ouverte sur un téléphone, quand la synchronisation d'horloge est faite, alors elle affiche l'aller-retour estimé (RTT) et l'incertitude qui en découle sur l'heure serveur (la moitié du RTT), en millisecondes.
- Étant donné la page `/diagnostic/`, quand on touche « Flash synchronisé », alors l'écran passe en plein écran blanc pendant 100 ms à chaque seconde entière de l'heure serveur estimée, jusqu'à ce qu'on touche de nouveau l'écran.
- Étant donné plusieurs téléphones et l'écran TV posés côte à côte en mode flash, quand on les filme au ralenti, alors on mesure l'écart entre leurs flashs ; la mesure faite pour la phase 4 est consignée dans `docs/mesures/dispersion-horloge.md`.
- Étant donné la console GM, quand elle affiche la qualité réseau des joueurs (US-E12-04), alors l'incertitude d'horloge de chaque téléphone y figure, et un téléphone au-delà de 50 ms est signalé discrètement.
- Étant donné la page `/diagnostic/`, quand elle passe en mode flash, alors le flash est calé sur `requestAnimationFrame` pour ne pas dériver, et l'horloge est resynchronisée périodiquement comme sur les autres pages.

**Comportement en cas d'erreur**
Si la synchronisation d'horloge échoue, la page l'indique en français, sans détail technique, et n'active pas le flash. Rien côté joueurs inscrits, public ou GM.

**Notes techniques**
- Aucune intention ni projection nouvelle pour le flash (décision 6 du README) : il dépend uniquement de `ClockSync`.
- L'incertitude d'horloge se déduit du RTT déjà remonté par `ReportConnectionQuality` (`ConnectionQuality`) : pas de nouveau contrat.
- Le seuil de 50 ms est une valeur de départ.

**Hors périmètre**
- Une mesure automatique de la dispersion, sans caméra.
