### US-E05-02 — Indicateur de coupure et interactions verrouillées

**Statut :** À faire

**En tant que** joueur
**je veux** être prévenu discrètement quand ma connexion est perdue, et ne pas pouvoir agir sur un écran périmé
**afin de** ne pas croire qu'une action a été prise en compte alors qu'elle ne pouvait pas l'être

**Critères d'acceptation**
- Étant donné une coupure de moins de 3 s, quand la connexion revient, alors rien n'a été affiché.
- Étant donné une coupure de plus de 3 s, quand le délai est écoulé, alors un indicateur discret (« Reconnexion… ») apparaît, sans masquer le contenu ni le décaler.
- Étant donné une coupure, quelle que soit sa durée, quand elle commence, alors tous les éléments interactifs (boutons, champs) sont désactivés, et le restent jusqu'à la réception d'un snapshot frais après la réidentification.
- Étant donné la connexion revenue, quand le snapshot frais est reçu, alors l'indicateur disparaît et les interactions sont réactivées.
- Étant donné l'écran TV, quand il perd la connexion plus de 3 s, alors il conserve son affichage, avec un indicateur discret placé hors des zones rognées par les TV.
- Étant donné l'interface GM, quand elle perd la connexion, alors elle se comporte comme l'interface joueur.
- Étant donné la saisie d'un pseudo ou d'un code GM en cours, quand une coupure survient, alors la saisie est conservée.
- Étant donné les tests Vitest, quand ils s'exécutent, alors ils couvrent les transitions de l'état de connexion (connecté, coupé, coupé depuis plus de 3 s, resynchronisé) avec des timers simulés. Un test E2E vérifie l'indicateur et le verrouillage avec `context.setOffline`.

**Comportement en cas d'erreur**
C'est l'objet de l'US. Aucun message technique n'est jamais affiché, quel que soit le rôle.

**Notes techniques**
- L'état de connexion (`connected`, `reconnecting`, `stale`) est exposé par `shared/connection` dans un module `.svelte.ts`. L'état « frais » ne revient qu'avec un snapshot reçu après la réidentification, et non au simple rétablissement du WebSocket (CLAUDE.md, « Mise en veille »).
- Un composant partagé (`ConnectionIndicator.svelte`) est utilisé par les trois interfaces ; les vues désactivent leurs éléments à partir d'un seul dérivé, sans logique propre.
- Le délai de 3 s est mesuré avec `performance.now()` ou un timer, pas avec l'horloge murale.
- Textes dans `fr.ts`.

**Hors périmètre**
- File d'envoi des intentions et interface optimiste (E08).
- Remontée au serveur des erreurs clients (E10).
