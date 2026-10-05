### US-E13-05 — Jugement, réouverture du buzzer et révélation

**Statut :** À faire

**En tant que** game master
**je veux** valider ou refuser la réponse orale du joueur qui a la main, puis rouvrir le buzzer aux autres
**afin que** la question se joue jusqu'à ce que quelqu'un trouve, ou que je la révèle

**Critères d'acceptation**
- Étant donné un gagnant qui a la main, quand le GM valide sa réponse (`buzzer.judge`, qui nomme la question et l'ouverture), alors le joueur gagne les points de la manche et la question passe à la révélation.
- Étant donné un gagnant qui a la main, quand le GM refuse sa réponse, alors le joueur est bloqué pour la question, sans perdre de points (décision 3 du README) ; son téléphone affiche « Bloqué pour cette question », et le buzzer se rouvre aussitôt pour les autres.
- Étant donné un double appui du GM ou deux consoles ouvertes, quand le même jugement arrive deux fois, alors le second est rejeté comme obsolète : un joueur ne gagne jamais deux fois, et un refus ne bloque jamais le gagnant suivant.
- Étant donné un buzzer ouvert ou un gagnant qui a la main, quand le GM choisit « Révéler la réponse », alors la question passe à la révélation sans points.
- Étant donné une question où tous les joueurs connectés sont bloqués, quand le dernier est refusé, alors le buzzer reste fermé et la console GM ne propose plus que « Révéler la réponse ».
- Étant donné la révélation, quand elle s'affiche, alors la TV montre la réponse attendue et, s'il y en a un, le pseudo du joueur qui a trouvé ; chaque téléphone montre si son joueur a gagné des points. Le GM passe à la question suivante (`buzzer.nextQuestion`), puis, après la dernière, la manche se termine et le classement intermédiaire s'affiche (US-E09-02).
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent chaque jugement et chaque rejet (jugement sans gagnant, jugement obsolète, question déjà révélée), le blocage et la réouverture, la non-fuite de la réponse jusqu'à la révélation, et le scénario E2E d'un refus suivi d'une bonne réponse.

**Comportement en cas d'erreur**
Joueurs et public : aucun message. GM : un jugement rejeté ne change rien ; la console affiche l'état du snapshot suivant.

**Notes techniques**
- Les points s'ajoutent aux scores à la révélation (décision 3 du README de E09), pour le joueur dont la réponse a été validée.
- Réouverture : nouvelle ouverture numérotée de l'arbitrage (`Reopen`), qui écarte tout buzz de l'ouverture précédente encore en transit.
- Mettre à jour `docs/modes/buzzer.md`.

**Hors périmètre**
- Points négatifs ou blocage temporaire (décision 3 du README).
- Annuler un jugement : prévu avec les contrôles avancés du GM (E19).
