### US-E13-02 — Arbitrage du buzz

**Statut :** Terminée

Le moteur dispose d'un arbitrage de buzzer réutilisable par tous les modes : il désigne le gagnant d'après l'horodatage des appuis, après une fenêtre d'attente, et résiste aux horloges déréglées comme aux messages forgés.

**Critères d'acceptation**
- Étant donné un buzzer ouvert, quand le premier buzz arrive, alors un timer d'arbitrage est programmé (250 ms par défaut, `Buzzer:ArbitrationMilliseconds`, de 0 à 1 000). Les buzz reçus pendant la fenêtre sont retenus.
- Étant donné la fin de la fenêtre, quand le timer expire, alors le gagnant est le buzz à l'horodatage le plus ancien, même s'il est arrivé après un autre. À horodatage égal, le premier reçu l'emporte.
- Étant donné un horodatage postérieur à sa réception, quand le buzz est retenu, alors il compte pour son heure de réception : une horloge en avance ne pénalise pas son joueur au-delà de l'arrivée réelle de son buzz.
- Étant donné un horodatage antérieur à l'ouverture du buzzer, ou de plus d'une seconde antérieur à sa réception, quand le buzz est retenu, alors il est ramené à la plus tardive de ces deux bornes. Un appui avant l'ouverture ne peut pas gagner contre un appui fait après.
- Étant donné un buzz reçu après la désignation du gagnant, quand il arrive, alors il est rejeté (`BuzzerClosed`), sans effet.
- Étant donné un joueur bloqué, ou un second buzz du même joueur pendant la même ouverture, quand il arrive, alors il est rejeté (`PlayerBlocked`, `AlreadyBuzzed`).
- Étant donné un buzz qui nomme une autre question ou une autre ouverture que l'ouverture en cours, quand il arrive, alors il est rejeté comme obsolète : un buzz renvoyé après une reconnexion ne peut pas gagner une ouverture suivante.
- Étant donné une partie enregistrée pendant la fenêtre d'arbitrage, quand elle reprend, alors les buzz retenus sont conservés et le timer est reprogrammé avec ce qui restait de la fenêtre (US-E11-03).
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent avec `FakeTimeProvider` : gagnant arrivé second, égalité, bornage des horodatages, buzz tardif, joueur bloqué, double buzz, buzz obsolète et reprise pendant la fenêtre.

**Comportement en cas d'erreur**
Un buzz rejeté ne se voit nulle part : le snapshot suivant remet le bouton du joueur dans l'état décidé par le serveur, sans message.

**Notes techniques**
- Une brique pure du moteur, `PartyGame.Engine/Buzzers` : un `record` d'état (ouverture numérotée, instant d'ouverture, buzz retenus, joueurs bloqués, gagnant) et ses fonctions (`Open`, `Buzz`, `Arbitrate`, `Block`, `Reopen`), que chaque mode intègre à l'état de sa manche et appelle depuis `Handle`. Le timer reste celui de la manche (`ScheduleTimer`).
- Le buzz porte l'étape qu'il vise (question, numéro d'ouverture) et l'instant d'appui en millisecondes depuis l'époque Unix en heure serveur. La réception (`ReceivedAt`) vient de `context.Now`.
- La durée de la fenêtre arrive au moteur par le contexte (`GameContext`), lue de l'option typée `BuzzerOptions` côté serveur.
- Nouveaux motifs de rejet dans `RejectionReason`, journalisés en `Debug`.
- Les horodatages des buzz ne sont pas secrets, mais ne figurent dans aucune projection : seul le gagnant est public.

**Hors périmètre**
- L'affichage du gagnant (US-E13-04) et la réouverture après un refus (US-E13-05).
