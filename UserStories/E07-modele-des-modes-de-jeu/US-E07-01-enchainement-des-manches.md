### US-E07-01 — Manches du pack enchaînées par le moteur

**Statut :** Prête

**En tant que** game master
**je veux** que le lancement démarre la première manche du pack choisi, puis que la partie enchaîne ses manches jusqu'à la fin
**afin de** dérouler toute la soirée depuis ma console

**Critères d'acceptation**
- Étant donné un pack dont l'état contient les manches, quand le GM lance la partie, alors la première manche démarre, jouée par le mode que désigne son `type`, et chaque rôle reçoit la vue de la manche produite par ce mode, avec le numéro de la manche, le nombre total de manches et son titre.
- Étant donné une manche en cours, quand un joueur ou le GM envoie une intention propre à la manche, alors le moteur la transmet au mode de la manche en cours, avec l'état de la manche. Hors d'une manche, elle est rejetée sans effet.
- Étant donné une manche que son mode déclare terminée, quand ce n'est pas la dernière, alors la partie passe entre deux manches. Quand c'est la dernière, la partie est terminée.
- Étant donné la partie entre deux manches, quand le GM demande la manche suivante en nommant la manche qui vient de finir, alors la manche suivante démarre. Une seconde demande pour la même manche (double appui, deuxième console, renvoi après reconnexion) est rejetée sans effet (décision 1 du README).
- Étant donné un timer programmé par une manche, quand il échoit après la fin de cette manche, alors il est rejeté sans effet : chaque timer porte l'identifiant de sa manche.
- Étant donné un joueur qui rejoint pendant une manche, quand il est inscrit, alors il reçoit la vue de la manche en cours. Sa participation est décidée par le mode (US-E08-02).
- Étant donné la phase provisoire `Started`, quand cette US est réalisée, alors elle disparaît au profit des phases `Lobby`, `Round` (manche en cours), `BetweenRounds` et `Finished`. Les écrans provisoires de E04 sont remplacés par ceux de US-E07-02.
- Étant donné les tests du moteur, quand ils s'exécutent, alors un mode factice, enregistré pour le test, couvre : le lancement vers la première manche, le passage entre deux manches, la manche suivante, la fin de partie après la dernière manche, le rejet d'une intention hors manche, le rejet d'une manche suivante en double ou mal ciblée, et le rejet d'un timer d'une manche terminée.
- Étant donné l'ajout du mode quiz (E08), quand il est réalisé, alors il ne modifie ni `GameEngine`, ni `GameLoop`, ni `GameHub`, en dehors de l'enregistrement du mode dans `AddGameModes()` et des attributs `[JsonDerivedType]` de `PartyGame.Contracts`.

**Comportement en cas d'erreur**
Défaut : rien côté joueurs et public. Une exception levée par un mode est rattrapée par `GameLoop`, qui conserve l'état précédent ; le retour à l'état précédent avec incident GM et la proposition de passer la manche sont l'objet de E10. GM : une demande de manche suivante refusée comme obsolète ne produit aucun message, le snapshot montre déjà la manche en cours.

**Notes techniques**
- Interface `IGameMode` dans `PartyGame.Engine`, indicative :
  - le type d'activité qu'il joue ;
  - la vérification de la cohérence d'un descripteur, qui retourne des `PackProblem` (utilisée par US-E06-02) ;
  - le démarrage d'une manche à partir de son descripteur, qui produit l'état initial de la manche et ses effets ;
  - le traitement d'une entrée de la manche, sur le modèle de `Handle` : nouvel état de la manche, effets, motif de rejet, fin de manche, points attribués (E09) ;
  - la projection de l'état de la manche pour chaque rôle, et pour chaque joueur.
- Enregistrement explicite dans `AddGameModes()`, sans scan réflexif (conventions).
- L'état d'une manche est un `record` immuable propre au mode, dérivé d'une base commune, sérialisable pour la persistance (E11). Le contexte (`context.Now`, `context.Random`) est fourni au mode comme au moteur.
- Contrats : les vues de manche sont des bases polymorphes, une par rôle (`PlayerRoundView`, `DisplayRoundView`, `GameMasterRoundView`), présentes dans les snapshots pendant une manche. Les intentions de manche sont deux bases polymorphes, une pour les joueurs et une pour le GM. Le hub expose deux méthodes génériques pour les recevoir, celle du GM marquée `[GameMasterOnly]` : un mode n'ajoute aucune méthode au hub.
- Intention GM générique `NextRound(afterRound)`, refusée si `afterRound` n'est pas la manche qui vient de finir.
- Identifiant typé `RoundId` pour les manches. Les timers de manche le portent, et le moteur rejette ceux d'une manche terminée avant de solliciter le mode.
- Tant que US-E06-03 n'est pas réalisée, l'état peut recevoir ses manches dans les tests seulement : la démonstration sur l'application réelle passe par le choix du pack.

**Hors périmètre**
- Les vues client des modes (US-E07-02).
- Les scores et les classements (E09).
- L'écran d'introduction des manches (E18).
- La pause, le saut et le réordonnancement des manches (E19).
