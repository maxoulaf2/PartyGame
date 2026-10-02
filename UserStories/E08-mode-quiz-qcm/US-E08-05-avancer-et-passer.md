### US-E08-05 — Question suivante, question passée et fin de manche

**Statut :** À faire

**En tant que** game master
**je veux** passer à la question suivante quand la salle est prête, ou passer une question qui pose problème
**afin de** garder la main sur le rythme de la manche

**Critères d'acceptation**
- Étant donné une question révélée, quand le GM appuie sur « Question suivante », alors la question suivante de la manche passe en présentation sur tous les écrans.
- Étant donné la dernière question de la manche révélée, quand la console l'affiche, alors le bouton devient « Terminer la manche ». Son appui termine la manche, et la partie passe entre deux manches, ou se termine si c'était la dernière (US-E07-01).
- Étant donné une question en présentation, en réponses ouvertes ou verrouillée, quand le GM appuie sur « Passer la question » et confirme, alors la question est abandonnée : aucun point n'est attribué, les réponses reçues sont ignorées, le timer est annulé, et la manche continue avec la question suivante, ou se termine si c'était la dernière.
- Étant donné une question passée, quand la question suivante s'affiche, alors sa numérotation tient compte de la question passée (« Question 4/5 » après la 3 passée). La TV et les téléphones n'affichent aucun message au sujet de la question passée.
- Étant donné deux consoles GM, ou un renvoi après une reconnexion, quand deux intentions visent la même question (question suivante, passer la question), alors la seconde est rejetée : une question n'est jamais sautée par erreur.
- Étant donné les tests du moteur, quand ils s'exécutent, alors ils couvrent la question suivante, la fin de manche, la question passée dans chacune des trois phases, et chaque rejet : mauvaise phase, question mal ciblée, intention en double. Un test E2E enchaîne deux questions, puis en passe une.

**Comportement en cas d'erreur**
Joueurs et public : rien, l'écran suivant arrive avec le snapshot. GM : connexion perdue, la console est verrouillée (US-E05-02) ; un appui envoyé juste avant la coupure peut être renvoyé sans risque (décision 1 du README de E07).

**Notes techniques**
- Intentions GM `NextQuestion(round, question)` et `SkipQuestion(round, question)`. « Terminer la manche » est `NextQuestion` sur la dernière question : le mode déclare alors la manche terminée.
- La confirmation de « Passer la question » utilise `ConfirmDialog.svelte` (US-E04-05).
- Une question passée reste dans l'état de la manche, marquée comme telle, pour l'historique et la persistance (E11).
- Effet `CancelTimer` si la question passée avait ses réponses ouvertes.

**Hors périmètre**
- Le retour à une question précédente.
- Le saut d'une manche entière, la pause et le réordonnancement (E19).
- La proposition automatique de passer une question qui échoue de façon répétée (E10).
