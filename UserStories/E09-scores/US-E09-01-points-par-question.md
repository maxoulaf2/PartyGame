### US-E09-01 — Points gagnés à chaque question

**Statut :** À faire

**En tant que** joueur
**je veux** voir à chaque révélation les points que je viens de gagner et mon total
**afin de** savoir où j'en suis et d'avoir envie de répondre vite

**Critères d'acceptation**
- Étant donné une manche sans bonus de rapidité, quand une question est révélée, alors chaque joueur qui a choisi la bonne proposition gagne les points de la manche. Les autres gagnent 0.
- Étant donné une manche avec bonus de rapidité, quand une question est révélée, alors chaque bonne réponse rapporte en plus le bonus maximal multiplié par le temps restant à sa réception, divisé par la durée de réponse, arrondi à l'entier (décisions 1 et 2 du README).
- Étant donné une bonne réponse reçue à l'instant de l'ouverture, quand elle est notée, alors elle rapporte le bonus complet ; reçue à l'échéance, elle n'en rapporte aucun.
- Étant donné une question passée avant sa révélation, quand la manche continue, alors aucun point n'a été attribué pour elle (décision 3 du README).
- Étant donné la révélation, quand un téléphone l'affiche, alors il montre, sous le verdict, les points gagnés (« +1 350 ») et le nouveau total du joueur.
- Étant donné la console GM, quand elle s'affiche à tout moment de la partie, alors elle montre le score de chaque joueur. À la révélation, elle montre aussi les points gagnés à la question.
- Étant donné un joueur arrivé en cours de partie, quand il apparaît dans les scores, alors il part de 0 (décision 4 du README).
- Étant donné les projections `Display` et `Player` avant la révélation, quand les tests de non-fuite s'exécutent, alors elles sont identiques, que les réponses déjà reçues soient bonnes ou mauvaises : aucun score, total ou rang ne bouge avant la révélation.
- Étant donné les tests du moteur, quand ils s'exécutent, alors ils couvrent : bonne et mauvaise réponse, absence de réponse, bonus à l'ouverture, au milieu et à l'échéance, durée surchargée par la question, question passée, joueur arrivé en cours de partie, et cumul sur plusieurs questions et plusieurs manches.

**Comportement en cas d'erreur**
Défaut : rien côté joueurs et public. Les scores sont calculés uniquement par le serveur : un client n'additionne jamais rien, et affiche ce que contient le snapshot.

**Notes techniques**
- Les scores cumulés appartiennent à l'état de la partie, et non à celui d'une manche : le moteur les tient pour tous les modes. Un mode ne fait que déclarer les points qu'il attribue, quand il les attribue (US-E07-01).
- Les points gagnés à la dernière question révélée font partie de la vue de manche, pour l'affichage du téléphone et de la console.
- Calcul en entiers, sans flottant, pour un résultat exact et reproductible.
- Les nombres sont formatés à la française (« 1 350 ») par une fonction partagée du client.

**Hors périmètre**
- Les classements (US-E09-02, US-E09-03).
- L'ajustement manuel des scores par le GM (E19).
- Les équipes (phase 6).
