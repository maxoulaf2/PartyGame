### US-E08-06 — Renvoi des intentions après une coupure

**Statut :** À faire

**En tant que** joueur
**je veux** que ma réponse compte même si mon téléphone a perdu la connexion juste après mon choix
**afin de** ne jamais perdre de points à cause du Wi-Fi ou d'une mise en veille

**Critères d'acceptation**
- Étant donné un joueur, quand il envoie une intention, alors elle porte un `ClientSeq`, entier croissant conservé avec son jeton dans le `localStorage`.
- Étant donné une intention envoyée dont l'accusé de réception n'est pas arrivé (connexion perdue juste après l'envoi), quand le téléphone se reconnecte et se réidentifie, alors il la renvoie, avec le même `ClientSeq`.
- Étant donné une intention déjà traitée, quand elle arrive de nouveau avec le même `ClientSeq`, ou un `ClientSeq` inférieur, alors le serveur l'ignore : elle n'est jamais traitée deux fois.
- Étant donné une réponse à la question 3 restée en attente, quand elle est renvoyée alors que la question 4 est ouverte, alors elle est rejetée et ne compte pour aucune question.
- Étant donné une réponse renvoyée alors que la question est encore ouverte, quand le serveur la reçoit, alors elle est acceptée. Pour le bonus de rapidité, c'est l'heure de sa réception effective qui compte.
- Étant donné une page rechargée avec une intention en attente, quand elle redémarre, alors l'intention est toujours en file et renvoyée, et le choix reste affiché « en attente » jusqu'au snapshot frais.
- Étant donné une intention en attente et un snapshot frais qui la contredit (réponse refusée), quand il arrive, alors le snapshot l'emporte, sans message.
- Étant donné un jeton inconnu du serveur (`SessionUnknown`), quand le joueur s'inscrit de nouveau, alors la file et le `ClientSeq` repartent de zéro avec le nouveau jeton.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent : en Vitest, la file d'envoi (acquittement, renvoi dans l'ordre, persistance, remise à zéro) ; en intégration, le même `ClientSeq` reçu deux fois et traité une fois ; en E2E, une réponse dont l'acquittement est perdu, puis comptée une seule fois après la reconnexion.

**Comportement en cas d'erreur**
C'est l'objet de l'US. Joueur : aucun message, l'état « en attente » est confirmé ou effacé par le snapshot. Public et GM : rien, ils ne voient la réponse qu'une fois traitée. Le GM n'a pas de file : ses intentions nomment l'étape visée et peuvent être renvoyées sans risque (décision 1 du README de E07).

**Notes techniques**
- File d'envoi dans `shared/connection`, persistée dans le `localStorage` avec le jeton. Elle attend l'acquittement d'une intention avant d'envoyer la suivante, pour garder l'ordre.
- L'acquittement est le retour de l'invocation du hub. Une intention rejetée par le moteur est aussi acquittée : elle sort de la file.
- Dernier `ClientSeq` traité par joueur conservé dans l'état de la partie, donc persisté avec lui (E11). Le contrôle se fait dans le moteur, avant de transmettre l'intention au mode. Un rejet pour `ClientSeq` déjà traité est journalisé en `Debug`.
- Les intentions d'inscription et de reprise de session (`JoinGame`, `ResumeSession`) sont des requêtes avec réponse, antérieures à l'identité : elles n'ont pas de `ClientSeq`.
- Le renvoi a lieu après la réidentification, et l'interface reste verrouillée jusqu'au snapshot frais (US-E05-02).
- E2E : l'acquittement perdu se simule en coupant la WebSocket relayée (`page.routeWebSocket`) juste après l'envoi.

**Hors périmètre**
- La remontée des erreurs client (E10).
- La reprise après un crash du serveur (E11).
