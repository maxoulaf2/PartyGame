### US-E05-03 — Synchronisation d'horloge avec le serveur

**Statut :** À faire

**Résultat attendu**
Chaque client estime l'écart entre son horloge et celle du serveur, façon NTP, et expose une fonction qui convertit un instant local (`performance.now()`) en heure serveur et inversement. Les comptes à rebours (E08), le buzzer (E13) et la lecture audio synchronisée (E14) en dépendent.

**Critères d'acceptation**
- Étant donné le hub, quand un client appelle la méthode de synchronisation, alors le serveur répond avec son heure courante, lue sur le `TimeProvider` injecté, sans passer par la file de la partie.
- Étant donné un client connecté, quand la synchronisation s'exécute, alors il effectue une salve de 8 allers-retours et retient l'estimation issue des échantillons au RTT minimal (décision 2 du README).
- Étant donné un client synchronisé, quand 60 s se sont écoulées ou qu'une reconnexion a eu lieu, alors il se resynchronise.
- Étant donné le module de synchronisation, quand il est interrogé, alors il expose `serverNow()`, la conversion d'un horodatage `performance.now()` en heure serveur, ainsi que l'écart et le RTT estimés, et un indicateur « synchronisé ».
- Étant donné un changement de l'horloge système du téléphone pendant la partie, quand la conversion est utilisée, alors elle n'en est pas affectée : les calculs reposent sur `performance.now()` et non sur `Date.now()`.
- Étant donné des échantillons simulés (RTT variables, valeurs aberrantes, asymétrie), quand les tests Vitest s'exécutent, alors l'estimation retenue est celle attendue.
- Étant donné les logs du serveur, quand des synchronisations ont lieu, alors aucun log `Information` n'est écrit pour elles.
- Étant donné les tests d'intégration, quand ils s'exécutent avec `FakeTimeProvider`, alors la méthode du hub renvoie l'heure du `TimeProvider`.

**Comportement en cas d'erreur**
Sans objet pour les utilisateurs en phase 1 : un aller-retour perdu est ignoré, et une salve incomplète conserve l'estimation précédente. Aucune interface n'affiche encore de compte à rebours.

**Notes techniques**
- La synchronisation est une lecture de l'horloge, sans rapport avec l'état de la partie : elle ne transite pas par `GameLoop`, dont la file ajouterait une latence variable qui fausserait la mesure.
- Calcul classique : pour chaque échantillon `t0` (envoi), `ts` (heure serveur), `t1` (réception), RTT = `t1 - t0` et écart = `ts - (t0 + t1) / 2`. Les instants client sont pris avec `performance.now()` et rapportés à `performance.timeOrigin`.
- La salve est espacée (par exemple 50 ms entre deux envois) pour ne pas saturer le Wi-Fi.
- L'heure serveur circule en millisecondes depuis l'époque Unix, un entier simple à manipuler côté client.
- La page de diagnostic d'horloge (écart et RTT de chaque téléphone, flash synchronisé) est l'objet de E13.

**Hors périmètre**
- Page de diagnostic d'horloge (E13).
- Compte à rebours affiché (E08) et horodatage du buzz (E13).
