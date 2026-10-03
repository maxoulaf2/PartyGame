### US-E09-03 — Classement final

**Statut :** Terminée

**En tant que** public
**je veux** voir le classement final et le podium à la fin de la partie
**afin de** célébrer les gagnants et conclure la soirée

**Critères d'acceptation**
- Étant donné la dernière manche terminée, quand la partie passe en phase `Finished`, alors la TV affiche le classement final : un podium pour les trois premiers rangs, puis le reste du classement avec rang, pseudo et score.
- Étant donné le podium, quand la TV l'affiche, alors chaque marche se distingue par son rang écrit et une forme ou une hauteur, jamais par la couleur seule. Des ex aequo partagent la même marche.
- Étant donné 20 joueurs aux pseudos de 16 caractères, quand la TV affiche le classement final en 1080p, alors tous sont lisibles à 3 m, sans défilement, et rien d'essentiel ne se trouve à moins de 5 % d'un bord.
- Étant donné un téléphone de joueur, quand la partie est terminée, alors il affiche le rang final du joueur, ex aequo le cas échéant, et son score, avec un message adapté au podium ou non.
- Étant donné la console GM, quand la partie est terminée, alors elle affiche le classement final et l'état « Partie terminée ». Aucune intention de jeu n'est plus acceptée.
- Étant donné un téléphone qui rejoint une partie terminée, quand il s'inscrit (les inscriptions restent ouvertes, décision 3 du README de E04), alors il voit l'écran de fin, sans rang.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent dans le moteur le passage en `Finished` après la dernière manche et le rejet des intentions de jeu, et les projections des trois rôles. Un test E2E joue une partie complète avec le pack de test : trois joueurs, la TV et le GM, du choix du pack au classement final.

**Comportement en cas d'erreur**
Défaut : rien côté joueurs et public. Un téléphone qui se reconnecte après la fin reçoit l'écran de fin.

**Notes techniques**
- Même calcul des rangs que US-E09-02, transmis dans les projections.
- Le classement final remplace l'écran provisoire « Partie terminée » de US-E07-02.
- Commencer une nouvelle partie demande, en phase 2, de redémarrer le serveur.
- Réalisation : contrats. `DisplaySnapshot.Ranking`, `GameMasterSnapshot.Ranking` et `PlayerSnapshot.Standing` sont remplis entre deux manches et une fois la partie terminée. `PlayerStanding` porte désormais `RankedCount`, le nombre de joueurs classés, sur lequel le téléphone écrit sa position : un joueur inscrit après la fin n'est pas classé (décision 7 du README), et `PlayerCount` ne convient donc plus.
- Réalisation : moteur. `Player.JoinedAfterEnd` marque un joueur inscrit en phase `Finished` ; `Snapshots` le tient à l'écart du classement, et son téléphone reçoit un `Standing` nul. Les intentions de jeu étaient déjà rejetées après la dernière manche (`NotInRound`, `NotBetweenRounds`, `GameAlreadyStarted`) : des tests le vérifient désormais pour la partie terminée. Correction au passage : `RoundFlow.HandlePlayerIntent` échouait quand un mode attribuait des points sur une intention de joueur (cas d'un futur buzzer, jamais du quiz, qui les attribue à la révélation).
- Réalisation : client. `shared/podium.ts` regroupe les joueurs des trois premiers rangs par marche (`splitFinalRanking`), sans rien trier ni classer, et dit si un rang est sur le podium (`isOnPodium`). TV : `display/FinalRankingScreen.svelte` remplace le lobby en fin de partie, avec « Partie terminée », « Classement final », le podium (1er au centre, 2e à gauche, 3e à droite, chaque marche avec son rang écrit, sa hauteur propre et le score commun de ses ex aequo), puis la suite du classement en grille comme entre deux manches ; plus de QR code. Une marche s'élargit avec ses ex aequo et passe sur plusieurs lignes quand la place manque. Un rang sauté par des ex aequo (1, 1, 3) n'a pas de marche. Téléphone : `player/FinalScreen.svelte`, avec la position finale, le score et « Bravo, tu es sur le podium ! » ou « Merci d'avoir joué ! », ou, pour un téléphone inscrit après la fin, un simple message de fin. Console GM : `RoundControl.svelte` affiche « Partie terminée » et le classement final (`gm/RankingList.svelte`, partagé avec l'entre-deux-manches), sans aucune action de jeu.
- Réalisation : tests. Moteur : `FinishedGameTests` (points conservés au passage en `Finished`, rejet de chaque intention de jeu, joueur inscrit après la fin marqué, et pas avant), `RankingSnapshotsTests` (classement et rang des trois rôles en fin de partie, joueur arrivé en cours de partie classé, joueur inscrit après la fin non classé), `PlayerIntentSequenceTests` (intention qui rapporte des points), et un scénario « late arrival » dans `SnapshotsLeakTests`. Hub : `RoundsTests` vérifie le classement final reçu par la TV, la console et chaque téléphone, sans pseudo d'un autre joueur côté téléphone, puis l'inscription après la fin. Vitest : `podium.test.ts`. E2E : `display.spec.ts` (podium et suite tels que reçus, ex aequo sur une même marche, disposition du podium, mise en page de 20 pseudos de 16 caractères en 1080p avec des scores presque tous distincts, tous ex aequo, un joueur devant 19 ex aequo, 18 ex aequo sur la troisième marche), `gm.spec.ts` (classement final sans action) et `launch.spec.ts`, qui joue désormais une partie complète jusqu'au classement final sur la TV, trois téléphones et la console, puis inscrit un téléphone après la fin.

**Hors périmètre**
- Une nouvelle partie sans redémarrer le serveur, et le retour au lobby (E19).
- Le podium animé, les jingles et la musique de fin (E20).
- L'historique des soirées et l'export des scores (idées non planifiées).
