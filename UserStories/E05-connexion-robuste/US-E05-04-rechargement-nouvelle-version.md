### US-E05-04 — Rechargement automatique sur nouvelle version

**Statut :** À faire

**En tant que** joueur
**je veux** que mon téléphone charge de lui-même la nouvelle version de l'application après une mise à jour du serveur
**afin de** ne jamais rester bloqué sur une page en cache incompatible avec le serveur

**Critères d'acceptation**
- Étant donné un build du front, quand il est produit par `npm run build`, alors il embarque un identifiant de build et écrit ce même identifiant dans `wwwroot`.
- Étant donné le serveur démarré, quand un client se connecte au hub, alors le serveur lui transmet l'identifiant de build qu'il a lu dans `wwwroot` au démarrage.
- Étant donné un client dont l'identifiant diffère de celui du serveur, quand il le reçoit, alors il recharge la page une seule fois, sans action de l'utilisateur. Le jeton dans le `localStorage` permet au joueur de retrouver sa place après le rechargement (US-E05-01).
- Étant donné un rechargement qui ne résout pas l'écart (cache tenace, proxy), quand l'écart persiste après le rechargement, alors le client ne recharge plus en boucle : il continue avec sa version et un log est remonté au serveur en `Warning`.
- Étant donné `npm run dev`, quand on développe, alors le contrôle est inactif (décision 3 du README).
- Étant donné un serveur sans identifiant de build (front non construit), quand un client se connecte, alors aucun rechargement n'est déclenché.
- Étant donné les tests, quand ils s'exécutent, alors un test Vitest couvre la décision de rechargement (identiques, différents, rechargement déjà tenté, identifiant absent), et un test d'intégration vérifie que le serveur transmet l'identifiant lu dans `wwwroot`.

**Comportement en cas d'erreur**
Joueurs, public et GM : le rechargement est le seul effet visible, sans message. Opérateur : un écart persistant est visible en `Warning` dans les logs.

**Notes techniques**
- L'identifiant est injecté par Vite (`define`) et écrit dans un petit fichier JSON de `wwwroot` (par exemple `build.json`), qui ne doit pas être servi avec un cache long : `index.html` et ce fichier sont servis sans cache, les ressources à empreinte gardent leur cache long (US-E01-03).
- Le serveur transmet l'identifiant dans un message d'accueil à la connexion, défini dans `IGameClient`.
- La protection contre la boucle de rechargement utilise le `sessionStorage` (un marqueur par identifiant cible).
- La remontée du `Warning` utilise un appel du hub minimal, en attendant `ReportClientError` (E10), ou est reportée à E10 si celui-ci est réalisé avant.

**Hors périmètre**
- Mise à jour du serveur lui-même pendant une soirée.
- Remontée générale des erreurs clients (E10).
