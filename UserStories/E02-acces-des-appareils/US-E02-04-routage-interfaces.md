### US-E02-04 — Routage des trois interfaces

**Statut :** Terminée

**En tant qu'**opérateur
**je veux** que chaque rôle ait une adresse courte, stable et tolérante aux fautes de frappe courantes
**afin d'**ouvrir l'écran TV et l'interface GM sans hésitation, et que les joueurs atterrissent toujours sur leur page

**Critères d'acceptation**
- Étant donné le serveur .NET servant le build, quand on ouvre `/`, `/display/` et `/gm/`, alors on obtient respectivement la page joueur, l'écran TV et l'interface GM.
- Étant donné les mêmes adresses sans barre oblique finale (`/display`, `/gm`), quand on les ouvre, alors le navigateur est redirigé vers `/display/` et `/gm/`.
- Étant donné une casse différente (`/Display`, `/GM/`), quand on l'ouvre sur Windows comme sous Linux (Raspberry Pi), alors on arrive sur la bonne page.
- Étant donné un chemin de page inconnu (`/rejoindre`, `/index`), quand un navigateur l'ouvre, alors il est redirigé vers la page joueur `/` plutôt que de voir une erreur 404 technique.
- Étant donné un chemin inconnu sous `/api`, `/hub`, `/media` ou `/assets`, quand on l'appelle, alors le serveur répond par une 404 sans redirection : un appel technique erroné ne reçoit jamais une page HTML.
- Étant donné les redirections, quand on inspecte les réponses, alors elles sont temporaires (302 ou 307), pour qu'aucun navigateur ne les mémorise si les routes évoluent.
- Étant donné `npm run dev`, quand on ouvre `/display` et `/gm` sur le port de Vite, alors on arrive aussi sur la bonne page.
- Étant donné les tests d'intégration du serveur, quand ils s'exécutent, alors chacune de ces règles est couverte.

**Comportement en cas d'erreur**
Joueurs : une adresse mal saisie mène à la page joueur, jamais à une page d'erreur. Public et GM : idem pour `/display` et `/gm` mal orthographiés en casse. Sans build du front, le comportement de US-E01-03 est conservé : 404 et avertissement au démarrage.

**Notes techniques**
- La règle de repli ne s'applique qu'aux requêtes `GET` qui acceptent du HTML. Les préfixes réservés sont définis à un seul endroit.
- La gestion de la casse ne doit pas désactiver la mise en cache des ressources à empreinte (US-E01-03).
- En développement, un petit middleware Vite (`configureServer`) suffit si Vite ne gère pas déjà ces redirections. Les tests E2E tournent sur `vite preview` : vérifier que le comportement y est identique.
- Les pages restent servies par les fichiers statiques. Pas de routage côté client.

**Hors périmètre**
- Protection de l'interface GM par le code GM (US-E02-05 pour le code, E03 pour son contrôle).
- Pages de diagnostic (E12, E13).
