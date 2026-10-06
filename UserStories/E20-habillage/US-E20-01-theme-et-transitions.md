### US-E20-01 — Thème visuel et transitions de l'écran TV

**Statut :** À faire

**En tant que** public
**je veux** un écran TV au style soigné, qui passe d'un écran à l'autre sans à-coups
**afin de** me sentir dans un jeu télévisé plutôt que devant une page web

**Critères d'acceptation**
- Étant donné les pages TV, joueur et GM, quand elles s'affichent, alors elles utilisent la police auto-hébergée et les variables du thème (`shared/theme.css`) ; aucune couleur ni police n'est écrite en dur dans un composant.
- Étant donné la TV, quand elle passe d'un écran à l'autre (lobby, introduction de manche, question, révélation, classement, podium, pause), alors l'écran sortant s'efface et l'entrant apparaît par une transition de moins de 600 ms, sans écran vide entre les deux.
- Étant donné la TV, quand elle affiche une révélation (bonne réponse au quiz, titre au blind test, bonnes réponses aux questions ouvertes), alors la réponse est mise en valeur par une animation brève.
- Étant donné la TV en 1080p sur un Raspberry Pi 4 sous Chromium, quand une transition se joue avec 20 joueurs inscrits, alors elle reste fluide, sans saccade visible.
- Étant donné un navigateur réglé sur `prefers-reduced-motion`, quand la TV change d'écran, alors elle passe d'un écran à l'autre sans mouvement, par un simple fondu.
- Étant donné une page chargée, quand on inspecte ses requêtes, alors aucune ne sort du réseau local : la police est servie par le serveur.
- Étant donné les tests, quand ils s'exécutent, alors un test E2E vérifie qu'aucune requête externe n'est émise par les trois pages, et que la police est bien chargée sur la TV.

**Comportement en cas d'erreur**
Défaut : si la police ne se charge pas, la pile de polices système prend le relais ; une transition qui échoue laisse l'écran entrant affiché.

**Notes techniques**
- Police au format `woff2` dans `client/src/assets/fonts/`, sous licence OFL, avec sa licence copiée à côté ; un ou deux styles seulement pour limiter le poids. `font-display: swap`.
- Transitions entre écrans avec les transitions de Svelte (`svelte/transition`), en `opacity` et `transform` uniquement ; aucune bibliothèque d'animation.
- Les vues des modes gardent leur contenu ; seul le conteneur de chaque page orchestre les transitions entre écrans, sans connaître les modes.

**Hors périmètre**
- Plusieurs thèmes au choix, ou un thème fourni par le pack.
- Les animations des classements, déjà couvertes par US-E18-02.
