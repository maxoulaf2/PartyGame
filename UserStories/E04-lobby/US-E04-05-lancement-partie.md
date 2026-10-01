### US-E04-05 — Lancement de la partie

**Statut :** À faire

**En tant que** game master
**je veux** lancer la partie quand les joueurs sont prêts
**afin que** tous les écrans passent ensemble du lobby au jeu

**Critères d'acceptation**
- Étant donné le lobby sans aucun joueur, quand le GM regarde le bouton « Lancer la partie », alors il est désactivé, avec une indication du nombre de joueurs requis (au moins 1).
- Étant donné au moins un joueur inscrit, quand le GM appuie sur « Lancer la partie », alors une confirmation lui est demandée, puis la partie passe en phase `Started`.
- Étant donné la partie lancée, quand les snapshots sont diffusés, alors la TV affiche « La partie commence », chaque téléphone affiche un écran d'attente correspondant, et l'interface GM affiche l'état « Partie en cours » : ces écrans provisoires seront remplacés par la première manche en E07.
- Étant donné la partie déjà lancée, quand une seconde intention de lancement arrive (double appui, deuxième GM), alors elle est rejetée sans effet.
- Étant donné la partie lancée, quand un nouveau téléphone rejoint (décision 3 du README), alors il est inscrit et voit l'écran d'attente de la partie en cours ; la TV et le GM le voient apparaître dans la liste des joueurs.
- Étant donné les tests du moteur, quand ils s'exécutent, alors ils couvrent le lancement, le refus sans joueur et le refus d'un double lancement. Un test E2E lance la partie et vérifie les trois interfaces.

**Comportement en cas d'erreur**
GM : connexion perdue, le bouton est désactivé jusqu'au snapshot frais suivant (US-E05-02). Une intention perdue pendant la coupure n'a aucun effet : le snapshot reçu à la reconnexion montre que la partie n'est pas lancée, et le GM appuie de nouveau. Le renvoi automatique des intentions non acquittées arrive en E08. Joueurs et public : ils passent à l'écran suivant à la réception du snapshot, sans message.

**Notes techniques**
- Intention GM `StartGame`. La phase `Started` est provisoire : E07 la remplacera par le déroulé des manches du pack.
- La confirmation est une simple boîte de dialogue de l'interface GM (composant Svelte, pas `window.confirm`, mal rendu sur mobile).
- Le minimum d'un joueur facilite les tests ; une valeur plus haute pourra être configurée plus tard.
- Tous les textes sont dans `fr.ts`.

**Hors périmètre**
- Choix du pack et première manche (E06, E07).
- Retour au lobby, pause et fin de partie (E19).
- Points de départ et participation à une manche d'un joueur arrivé en cours de partie (E08, E09).
