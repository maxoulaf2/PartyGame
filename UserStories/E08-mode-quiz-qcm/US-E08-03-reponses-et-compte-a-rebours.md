### US-E08-03 — Réponses ouvertes et compte à rebours

**Statut :** À faire

**En tant que** joueur
**je veux** choisir une proposition sur mon téléphone avant la fin du compte à rebours, et voir tout de suite que mon choix est pris en compte
**afin de** jouer vite et sans doute sur ma réponse

**Critères d'acceptation**
- Étant donné une question en présentation, quand le GM appuie sur « Ouvrir les réponses », alors la question passe en phase `Answering`, avec une échéance en heure serveur égale à l'instant d'ouverture plus sa durée de réponse.
- Étant donné les réponses ouvertes, quand la TV et les téléphones les affichent, alors ils montrent le temps restant, calculé à partir de l'échéance et de l'écart d'horloge (US-E05-03). Le compte à rebours est le même, à la seconde près, sur tous les écrans.
- Étant donné les réponses ouvertes, quand la TV les affiche, alors elle montre le nombre de réponses reçues sur le nombre de joueurs participants (« 7 / 9 »), sans indiquer quelle proposition a été choisie.
- Étant donné un joueur participant, quand il touche une proposition, alors elle s'affiche aussitôt comme « en attente » et les autres se désactivent, puis elle passe à « Réponse enregistrée » à la réception du snapshot qui la confirme.
- Étant donné un joueur qui a déjà répondu, quand une seconde intention de réponse arrive, alors elle est rejetée et seule la première compte (décision 3 du README).
- Étant donné l'échéance atteinte, quand le timer échoit, alors la question passe en phase `Locked`. La TV et les téléphones affichent « Temps écoulé ». Un téléphone sans réponse le montre, sans aucune proposition sélectionnée.
- Étant donné une réponse reçue par le serveur après l'échéance, mais avant le traitement du timer, quand le moteur la traite, alors elle est rejetée : c'est l'heure de réception qui compte, pas l'ordre de traitement.
- Étant donné la console GM pendant les réponses, quand elle s'affiche, alors elle montre le compte à rebours, la liste des joueurs avec, pour chacun, la proposition choisie ou l'absence de réponse, la répartition en direct, et le bouton « Verrouiller maintenant ». Quand tous les joueurs participants ont répondu, la console le signale.
- Étant donné le GM qui appuie sur « Verrouiller maintenant », quand l'intention est traitée, alors la question passe en `Locked` et le timer est annulé.
- Étant donné un joueur arrivé après l'ouverture, quand il voit la question, alors son téléphone indique qu'il jouera à la question suivante (décision 5 du README).
- Étant donné les projections en phase `Answering` et `Locked`, quand les tests de non-fuite s'exécutent, alors la projection `Display` est identique quels que soient les choix des joueurs (seul le nombre de réponses compte), et la projection `Player` d'un joueur est identique quels que soient les choix des autres.
- Étant donné les tests du moteur, quand ils s'exécutent, alors ils couvrent l'ouverture, la réponse acceptée, et chaque rejet : réponse hors de la phase `Answering`, seconde réponse, joueur non participant, proposition inconnue, réponse visant une autre question, réponse reçue après l'échéance, ouverture ou verrouillage en double ou mal ciblé. Un test E2E fait répondre trois joueurs, puis vérifie le compteur de la TV et la console GM.

**Comportement en cas d'erreur**
Joueur : si sa réponse est refusée (arrivée après l'échéance), l'état « en attente » disparaît au snapshot suivant, qui montre « Temps écoulé », sans aucun message : le snapshot l'emporte. Connexion perdue : les propositions sont verrouillées (US-E05-02), le renvoi d'une réponse non acquittée est l'objet de US-E08-06. Public : la TV garde le compte à rebours, calculé localement. GM : une intention refusée comme obsolète n'affiche rien.

**Notes techniques**
- Intention joueur `SubmitAnswer(round, question, choice)`, qui porte aussi le `ClientSeq` (US-E08-06). Elle nomme la question pour qu'une réponse retardée ne puisse jamais compter pour la suivante.
- Intentions GM `OpenAnswers(round, question)` et `LockAnswers(round, question)` (décision 1 du README de E07).
- Effets `ScheduleTimer` à l'ouverture et `CancelTimer` au verrouillage anticipé. L'heure de réception de chaque réponse (`ReceivedAt`) est conservée pour le bonus de rapidité (US-E09-01).
- Les participants d'une question sont les joueurs inscrits à l'ouverture des réponses, connectés ou non. Le compteur de la TV porte sur eux.
- Le compte à rebours affiché ne décide jamais rien (conventions) : il s'arrête à 0, et la phase ne change qu'avec le snapshot.
- Zones tactiles d'au moins 48 px et `touch-action: manipulation`. Le choix se fait au `click` : la règle du `pointerdown` ne concerne que le buzzer, et le `click` évite qu'un glissement du doigt ne valide une proposition par erreur.

**Hors périmètre**
- La révélation (US-E08-04) et les points (US-E09-01).
- Le renvoi des intentions non acquittées après une coupure (US-E08-06).
- Les sons de compte à rebours (E20).
