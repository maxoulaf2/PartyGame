### US-E09-02 — Classement entre deux manches

**Statut :** À faire

**En tant que** public
**je veux** voir le classement à la fin de chaque manche
**afin de** savoir qui mène avant d'attaquer la suivante

**Critères d'acceptation**
- Étant donné une manche terminée qui n'est pas la dernière, quand la partie passe entre deux manches, alors la TV affiche « Classement après la manche 1 », avec pour chaque joueur son rang, son pseudo et son score.
- Étant donné des joueurs à égalité, quand le classement est calculé, alors ils partagent le même rang, le rang suivant en tient compte (1, 1, 3), et ils sont affichés par ordre alphabétique de pseudo (décision 5 du README).
- Étant donné 20 joueurs aux pseudos de 16 caractères, quand la TV affiche le classement en 1080p, alors tous sont lisibles à 3 m, sans défilement, et rien d'essentiel ne se trouve à moins de 5 % d'un bord.
- Étant donné un téléphone de joueur, quand la partie est entre deux manches, alors il affiche le rang du joueur sur le nombre de joueurs, ex aequo le cas échéant (« 3e ex aequo sur 9 »), et son score.
- Étant donné la console GM, quand la partie est entre deux manches, alors elle affiche le classement et le bouton « Manche suivante », avec le titre de cette manche. Son appui la démarre (US-E07-01).
- Étant donné des joueurs déconnectés ou arrivés en cours de partie, quand le classement est affiché, alors ils y figurent comme les autres.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent le calcul des rangs (égalités comprises) dans le moteur et les projections des trois rôles. Un test E2E termine une manche et vérifie le classement sur la TV et sur un téléphone.

**Comportement en cas d'erreur**
Défaut : rien côté joueurs et public. Un téléphone qui se reconnecte entre deux manches reçoit le classement courant.

**Notes techniques**
- Les rangs sont calculés par le moteur et transmis dans les projections : le client ne trie ni ne classe rien.
- Le classement remplace l'écran provisoire « Fin de la manche » de US-E07-02. Il appartient aux pages et non à un mode : il vit dans `display/`, `player/` et `gm/`, pas dans `modes/`.
- La mise en page s'appuie sur celle de la liste du lobby (`display/playerListLayout.ts`) si elle s'y prête.
- Le classement n'est pas montré à la fin de la dernière manche : la partie passe directement au classement final (US-E09-03).

**Hors périmètre**
- Les animations de progression dans le classement (E20).
- Les classements en cours de manche.
