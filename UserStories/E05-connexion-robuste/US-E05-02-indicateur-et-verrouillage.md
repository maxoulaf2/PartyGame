### US-E05-02 — Indicateur de coupure et interactions verrouillées

**Statut :** Terminée

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
- Réalisation : `ConnectionStatus` (`shared/connection/connectionStatus.svelte.ts`) dérive l'état d'un seul booléen « synchronisé » fourni par la page : `connected` (synchronisé), `interrupted` (non synchronisé depuis moins de 3 s : verrouillé, rien d'affiché) et `reconnecting` (au-delà : indicateur). Un `$effect` arme un timer de 3 s quand la page cesse d'être synchronisée et l'annule dès qu'elle l'est de nouveau. Le booléen étant un `$derived`, une seconde perte de connexion pendant la même coupure ne relance pas le délai. Une page démarre non synchronisée : un premier démarrage de plus de 3 s affiche aussi l'indicateur.
- Réalisation : « synchronisé » vaut, pour le joueur (`PlayerSession.synchronized`), connecté et soit sur le formulaire d'inscription, qui n'attend aucun snapshot, soit avec un snapshot frais ; pour le GM (`GameMasterSession.synchronized`), connecté et soit sans accès encore, soit avec un snapshot frais ; pour la TV, un snapshot frais (`connectDisplay` marque désormais le snapshot périmé à la perte de la connexion). Un snapshot frais n'arrive qu'après la réidentification (reprise de session, annonce).
- Réalisation : `ConnectionIndicator.svelte`, commun aux trois pages, est en position fixe et ignore les touchers : il ne décale ni ne bloque rien. Sur téléphone, il s'affiche en haut au centre, sous la zone sûre ; sur la TV (`tv`), en haut à droite, dans la marge de 5 % qui échappe au rognage. Son conteneur `role="status"` reste dans la page pour que les lecteurs d'écran annoncent l'apparition du message.
- Réalisation : les pages passent `status.interactive` à leurs vues (prop `interactive`), qui en dérivent seules l'état de leurs boutons et de leurs champs. Les champs (pseudo, code GM, renommage) sont désormais désactivés eux aussi, et leur saisie est conservée. Le prop `fresh` des composants GM disparaît au profit de ce seul dérivé. Les boutons « Annuler », purement locaux, restent actifs. Le champ du code GM reprend le focus au retour de la connexion.
- Réalisation : Vitest exécute désormais les `$effect`, grâce à un environnement Node qui transforme les modules pour le client (`vite/svelteClientEnvironment.ts`), avec `svelte` résolu par Vite sous la condition `browser`. Auparavant, les modules `.svelte.ts` étaient compilés pour le serveur, où les effets ne s'exécutent jamais.
- Réalisation : tests Vitest (`connectionStatus.test.ts` : démarrage, premier démarrage long, verrouillage immédiat, coupure courte sans indicateur, indicateur au-delà de 3 s, délai non relancé par une seconde perte, retour du snapshot frais, coupures successives, arrêt ; `playerSession.test.ts`, `gameMasterSession.test.ts` et `displayConnection.test.ts` : état synchronisé et snapshot périmé) et Playwright (`e2e/reconnection.spec.ts`, sur iPhone et Pixel : formulaire joueur verrouillé avec le pseudo conservé, lobby joueur, console GM verrouillée puis déverrouillée, code GM conservé, écran TV conservé avec l'indicateur ; à chaque fois, l'indicateur est absent pendant 2 s puis présent après 3 s).

**Hors périmètre**
- File d'envoi des intentions et interface optimiste (E08).
- Remontée au serveur des erreurs clients (E10).
