### US-E08-04 — Révélation de la bonne réponse

**Statut :** À faire

**En tant que** public
**je veux** découvrir ensemble la bonne réponse et qui a choisi quoi
**afin de** partager le moment de la révélation et chambrer ceux qui se sont trompés

**Critères d'acceptation**
- Étant donné une question verrouillée, quand le GM appuie sur « Révéler », alors la question passe en phase `Revealed`. Le bouton n'existe pas tant que les réponses sont ouvertes : le GM verrouille d'abord (US-E08-03).
- Étant donné la révélation, quand la TV l'affiche, alors la bonne proposition est mise en évidence par une icône et un libellé en plus de la couleur, et les autres sont atténuées.
- Étant donné la révélation, quand la TV l'affiche, alors elle montre sous chaque proposition le nombre de joueurs qui l'ont choisie et leurs pseudos, puis les joueurs participants sans réponse (décision 4 du README).
- Étant donné 20 joueurs aux pseudos de 16 caractères, quand ils ont tous choisi la même proposition, alors leurs pseudos restent lisibles à 3 m sur la TV en 1080p, sans débordement ni défilement.
- Étant donné un téléphone de joueur, quand la réponse est révélée, alors il affiche « Bonne réponse ! », « Raté » avec la bonne proposition (lettre, forme et texte), ou « Pas de réponse ».
- Étant donné un joueur qui n'a pas participé (arrivé après l'ouverture), quand la réponse est révélée, alors son téléphone montre la bonne réponse, sans verdict.
- Étant donné la console GM, quand la réponse est révélée, alors elle affiche la même répartition que la TV et le bouton « Question suivante » (US-E08-05).
- Étant donné deux consoles GM, ou un renvoi après une reconnexion, quand deux intentions de révélation visent la même question, alors la seconde est rejetée sans effet.
- Étant donné la phase `Revealed`, quand les tests de non-fuite s'exécutent, alors la bonne réponse et les choix de tous les joueurs y sont publics : seule la phase `Revealed` les autorise.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent la révélation, son rejet hors de la phase `Locked` ou mal ciblée, et les projections des trois rôles. Un test E2E vérifie la répartition sur la TV et le verdict de chaque téléphone.

**Comportement en cas d'erreur**
Défaut : rien côté joueurs et public. Un téléphone qui se reconnecte pendant la révélation reçoit le snapshot courant, avec son verdict. GM : une révélation refusée comme obsolète n'affiche rien.

**Notes techniques**
- Intention GM `RevealAnswer(round, question)`.
- Les points gagnés s'ajoutent à l'écran du téléphone et à la console GM avec US-E09-01.
- Les pseudos sous chaque proposition suivent l'ordre d'arrivée des joueurs, comme sur le lobby, pour un affichage stable. La mise en page s'appuie sur celle de la liste du lobby (`display/playerListLayout.ts`) si elle s'y prête.
- Les pseudos sont affichés comme du texte, jamais interprétés comme du HTML (US-E04-03).

**Hors périmètre**
- Les points et le classement (E09).
- Les animations et les sons de révélation (E20).
