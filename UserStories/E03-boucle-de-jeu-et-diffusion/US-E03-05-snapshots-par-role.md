### US-E03-05 — Snapshots versionnés, projetés par rôle et diffusés

**Statut :** Terminée

**Résultat attendu**
Après chaque transition qui change l'état, le serveur envoie à chaque client l'état complet qui le concerne, projeté selon son rôle et versionné. Côté client, un store partagé conserve le dernier snapshot et ignore tout ce qui est plus ancien. Une connexion qui s'annonce reçoit immédiatement le snapshot courant.

**Critères d'acceptation**
- Étant donné `PartyGame.Contracts`, quand on l'inspecte, alors il contient trois DTO : `PlayerSnapshot`, `DisplaySnapshot` et `GameMasterSnapshot`, chacun avec `GameId` et `Version`. Les types TypeScript sont régénérés.
- Étant donné une transition qui change l'état, quand la boucle l'a appliquée, alors la version augmente de 1, la projection `Display` est envoyée au groupe `display`, la projection `GameMaster` au groupe `gm`, et chaque joueur reçoit sa propre projection `Player` dans son groupe.
- Étant donné une connexion qui vient de s'annoncer (TV, GM authentifié, joueur identifié), quand l'annonce est acceptée, alors elle reçoit aussitôt le snapshot courant de son rôle, sans attendre le prochain changement.
- Étant donné le store de snapshot côté client, quand il reçoit une version inférieure ou égale à celle qu'il affiche pour le même `GameId`, alors il l'ignore. Quand il reçoit un `GameId` différent, alors il adopte le nouveau snapshot quelle que soit sa version (décision 2 du README).
- Étant donné les trois interfaces, quand un snapshot arrive, alors leur affichage en dérive entièrement, sans aucune logique de jeu côté client.
- Étant donné chaque projection, quand les tests de non-fuite s'exécutent, alors ils vérifient qu'aucune ne contient de jeton de joueur ni le code GM, et qu'une projection `Player` ne contient que les informations destinées à ce joueur ou publiques.
- Étant donné le store, quand les tests Vitest s'exécutent, alors ils couvrent l'ordre des versions, le changement de `GameId` et l'arrivée d'un premier snapshot.

**Comportement en cas d'erreur**
Joueurs et public : si un envoi échoue (connexion coupée), le client garde le dernier snapshot affiché ; il recevra le snapshot courant à sa reconnexion (E05). Opérateur : un échec de diffusion est journalisé en `Warning` et n'interrompt pas la boucle.

**Notes techniques**
- Les projections sont des fonctions pures de l'état vers les DTO, testées sans réseau. L'état du moteur n'est jamais sérialisé directement vers un client.
- La version est portée par l'état (ou par la boucle, à condition d'être persistée avec lui en E11) : elle doit survivre à une reprise après crash.
- Le store vit dans un module `.svelte.ts` de `shared/connection`, en runes Svelte 5. Il expose aussi un indicateur « snapshot frais reçu », utilisé par US-E05-02.
- La diffusion est exécutée par la boucle après la transition et ses effets, protégée comme eux : elle implémente `IGameStateListener` (`PartyGame.Server/Games`), appelé après chaque transition qui change l'état.
- Le contenu des snapshots du lobby (joueurs, adresse, phase) est défini par E04. Cette US peut démarrer avec un contenu minimal (phase et nombre de joueurs).
- Le helper de test de non-fuite réutilisable par tous les modes est l'objet de E07 ; ici, des tests ciblés suffisent.
- Point d'accroche (US-E03-04) : les messages du serveur vers les clients s'ajoutent comme méthodes de `IGameClient` (`Contracts`), puis `npm run generate:contracts` ; le client s'y abonne par `GameConnection.on`. L'envoi du snapshot courant après une annonce acceptée se fait dans `GameHub.AnnounceAsync` ; les groupes sont nommés dans `HubGroups`. La TV se connecte et s'annonce par `createGameConnection` (`shared/connection/gameHub.ts`).
- Réalisation : `IGameClient` porte trois messages, `ReceiveDisplaySnapshot`, `ReceiveGameMasterSnapshot` et `ReceivePlayerSnapshot`. Les trois DTO portent `GameId`, `Version`, `Phase` (énumération de `Contracts`, distincte de `GamePhase` du moteur) et `PlayerCount` ; `PlayerSnapshot` y ajoute le `PlayerId` et le pseudo du joueur destinataire.
- Réalisation : la version est portée par `GameState.Version` (1 à la création). `GameLoop` l'incrémente de 1 pour chaque transition qui change l'état, quelle que soit la valeur posée par le moteur : aucun mode ne peut l'oublier, et elle sera persistée avec l'état (E11).
- Réalisation : les projections sont des fonctions pures de `PartyGame.Engine/Projections/Snapshots` (`ForDisplay`, `ForGameMaster`, `ForPlayer`), testées sans réseau, avec des tests de non-fuite (aucun jeton, rien sur les autres joueurs dans une projection `Player`).
- Réalisation : `SnapshotBroadcaster` (`PartyGame.Server/Hubs`) implémente `IGameStateListener` : il envoie la projection `Display` au groupe `display`, `GameMaster` au groupe `gm`, et à chaque joueur sa projection dans `HubGroups.Player(id)` (`player:<PlayerId>`). Un envoi qui échoue est journalisé en `Warning` et n'empêche pas les autres.
- Réalisation : `GameHub.AnnounceAsync` lit l'état courant après l'entrée dans le groupe, puis envoie le snapshot du rôle à l'appelant : un changement survenu entre-temps est aussi diffusé au groupe, et le client garde la version la plus récente. L'envoi du snapshot `Player` à l'identification d'un joueur viendra avec US-E04-02 et US-E05-01, aucun joueur ne pouvant encore s'identifier par le hub ; les tests placent une connexion dans un groupe de joueur par `IHubContext`.
- Réalisation : côté client, `SnapshotStore` (`shared/connection/snapshotStore.svelte.ts`) garde le dernier snapshot (`current`) et l'indicateur `fresh`. `markStale()` le remet à faux (à appeler par E05 lors d'une coupure) ; un snapshot plus récent, ou la même version renvoyée par le serveur après une reconnexion, le rétablit.
- Réalisation : l'écran TV se connecte par `connectDisplay` (`shared/connection/displayConnection.ts`) et affiche le nombre de joueurs inscrits (`countText`, `shared/i18n/countText.ts`). Un serveur injoignable laisse l'écran neutre. Les pages GM et joueur se connecteront avec l'écran du code (US-E04-01) et l'inscription (US-E04-02), en réutilisant le même store.
- Réalisation : vérifié sur le serveur .NET avec le front construit : la page `/display/` s'annonce et affiche « En attente des joueurs… » ; un client Node reçoit le snapshot courant en `Display` et en `GameMaster`, rien avec un mauvais code. La diffusion après une inscription est couverte par les tests d'intégration du hub, l'inscription n'étant pas encore exposée.

**Hors périmètre**
- Diffs entre snapshots (écartés par l'ADR 0001).
- Projections propres à un mode (E07, E08).
