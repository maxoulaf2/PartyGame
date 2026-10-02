### US-E09-03 — Classement final

**Statut :** À faire

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

**Hors périmètre**
- Une nouvelle partie sans redémarrer le serveur, et le retour au lobby (E19).
- Le podium animé, les jingles et la musique de fin (E20).
- L'historique des soirées et l'export des scores (idées non planifiées).
