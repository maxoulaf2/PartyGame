### US-E09-02 — Classement entre deux manches

**Statut :** Terminée

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
- Réalisation : contrats. `DisplaySnapshot.Ranking` et `GameMasterSnapshot.Ranking` portent tous les joueurs inscrits (`RankedPlayer` : identifiant, pseudo, connexion, rang, ex aequo, score), dans l'ordre du classement, entre deux manches ; ils sont vides dans les autres phases. `PlayerSnapshot.Standing` (`PlayerStanding` : rang et ex aequo) ne porte que le rang du joueur, sur `PlayerCount` joueurs, jamais les pseudos ni les scores des autres ; il vaut `null` hors de l'entre-deux-manches. `GameMasterSnapshot.NextRoundTitle` porte le titre de la manche suivante, réservé au GM : les autres rôles découvrent les manches au fil de la partie.
- Réalisation : moteur. `Ranking.Of` (`Engine/Scores`) trie par score décroissant ; le rang vaut 1 + le nombre de joueurs qui ont plus de points (1, 1, 3) et `IsTied` signale un rang partagé. À égalité, l'ordre alphabétique ignore accents et casse (`TextComparison.Key`), puis départage par point de code, pour un ordre indépendant de la culture de l'hôte. Les joueurs déconnectés et arrivés en cours de partie (0 point) sont classés comme les autres. `Snapshots` projette le classement en phase `BetweenRounds` seulement.
- Réalisation : client. Les rangs s'écrivent à la française (« 1er », « 2e ») et la position du joueur (« 3e ex aequo sur 9 ») avec `rankText` et `standingText` (`shared/i18n/rankText.ts`). TV : `display/RankingScreen.svelte` remplace le lobby entre deux manches, avec « Fin de la manche 1/2 », « Classement après la manche 1 », puis rang, pseudo et score de chaque joueur, les déconnectés atténués et marqués d'une icône ; la liste reprend `playerListLayout` et se remplit colonne par colonne. Un petit QR code reste en haut à droite pour les retardataires. Téléphone : `player/RankingScreen.svelte`, avec la position et le total du joueur. Console GM : `RoundControl.svelte` affiche la manche suivante (« Manche suivante (2/2) : Finale ») et son bouton, puis le classement, placé après pour que le bouton reste visible même avec beaucoup de joueurs.
- Réalisation : tests. Moteur : `RankingTests` (scores distincts, égalités en tête et au milieu, ordre alphabétique sans accents ni casse, joueur déconnecté et arrivé en cours de partie, aucun joueur) et `RankingSnapshotsTests` (classement de la TV et du GM, rang de chaque téléphone, retardataire entre deux manches, rien hors de l'entre-deux-manches, titre de la manche suivante pour le GM). Non-fuite : `SnapshotsLeakTests` ajoute un scénario d'égalités dans chaque phase, et une paire d'états qui ne diffèrent que par le score d'un autre joueur, identiques pour les téléphones des autres. Hub : `RoundsTests` vérifie le classement reçu par la TV, la console et un téléphone à la fin d'une manche, et l'absence du titre de la manche suivante dans le JSON de la TV. Vitest : `rankText.test.ts`. E2E : `display.spec.ts` (classement tel que reçu, pseudo non interprété comme HTML, joueur déconnecté, mise en page de 20 pseudos de 16 caractères en 1080p), `gm.spec.ts` (classement et manche suivante) et `launch.spec.ts` (fin de la première manche : classement sur la TV, position et total sur trois téléphones, classement et manche suivante sur la console).

**Hors périmètre**
- Les animations de progression dans le classement (E20).
- Les classements en cours de manche.
