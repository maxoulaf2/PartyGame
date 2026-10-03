### US-E08-06 — Renvoi des intentions après une coupure

**Statut :** Terminée

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
- Réalisation : contrats. `SendRoundIntent` reçoit une `PlayerIntentEnvelope` (`clientSeq`, `intent`), sans toucher aux intentions des modes (décision 11 du README). Le hub refuse comme malformée, en `Warning`, une enveloppe sans numéro ou dont le numéro est inférieur à 1, et copie le numéro dans `PlayerRoundInput.ClientSeq`.
- Réalisation : moteur. `Player.LastClientSeq` garde le numéro de la dernière intention acceptée du joueur, 0 au départ, et fait partie de l'état persisté. `RoundFlow` rejette avant le mode, avec `IntentAlreadyHandled`, une intention dont le numéro ne le dépasse pas, puis l'enregistre une fois l'intention acceptée, même quand le mode ne change rien. Une intention rejetée par le mode ne l'enregistre pas : l'état reste la même instance, sans diffusion, et l'intention renvoyée est jugée de nouveau, à l'heure de sa nouvelle réception. Les numéros peuvent donc sauter ceux des intentions rejetées.
- Réalisation : client. `IntentQueue` (`shared/connection/intentQueue.svelte.ts`) numérote et garde les intentions dans le `localStorage` (`partygame.player.intents`), avec le jeton et le dernier numéro donné. `PlayerSession` l'ouvre une fois le joueur identifié (inscription ou reprise de session acceptée), la ferme à chaque coupure, la reprend au démarrage pour le jeton conservé, la remet à zéro avec le jeton d'une nouvelle inscription et l'efface sur `SessionUnknown`. Une seule intention à la fois : la suivante part après l'acquittement de la précédente. Une intention envoyée par une connexion perdue est renvoyée par la suivante, même si l'ancienne n'a pas encore abandonné. Les vues des modes reçoivent `pending`, les intentions de la manche affichée non acquittées : le choix « en attente » du quiz en est dérivé, et survit donc à un rechargement ; `send` ne renvoie plus rien.
- Réalisation : tests. Moteur : `PlayerIntentSequenceTests` (numéro enregistré, renvoi et numéro plus ancien rejetés sans atteindre le mode, numéros qui sautent, intention acceptée sans changement, intention rejetée par le mode, numéros propres à chaque joueur) et `QuizResendTests` (réponse traitée puis renvoyée, réponse perdue renvoyée pendant les réponses et comptée à sa réception, réponse renvoyée à la question suivante et rejetée). Hub : `QuizAnswersTests` renvoie une réponse avec le même numéro depuis une nouvelle connexion, puis une copie avec un autre choix, sans aucun changement ; `RoundsTests` ajoute les enveloppes malformées. Vitest : `intentQueue.test.ts` (ordre, acquittement, renvoi, nouvelle connexion avant l'abandon de l'ancienne, persistance, jeton différent, valeur illisible, remise à zéro) et `playerSession.test.ts` (envoi après identification, renvoi avec le même numéro, rechargement, nouvelle inscription). E2E : dans `launch.spec.ts`, un téléphone dont la WebSocket est coupée juste après l'envoi de sa réponse la renvoie avec le même numéro, et la réponse est comptée une seule fois.

**Hors périmètre**
- La remontée des erreurs client (E10).
- La reprise après un crash du serveur (E11).
