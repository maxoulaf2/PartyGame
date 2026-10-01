### US-E03-04 — Hub SignalR typé, rôles et groupes

**Statut :** Terminée

**Résultat attendu**
Un hub SignalR fortement typé, exposé sous `/hub`, accepte les connexions des trois rôles et les range dans leurs groupes. Côté client, un module unique de `shared/connection` encapsule `@microsoft/signalr` : aucun composant ne l'importe directement. Le hub se contente de valider la forme des messages et de les déposer dans la file.

**Critères d'acceptation**
- Étant donné le serveur démarré, quand un client se connecte au hub sous `/hub/game`, alors la connexion aboutit en WebSocket, sur le serveur .NET comme à travers le proxy Vite (`npm run dev`).
- Étant donné une connexion de l'écran TV, quand elle s'annonce comme `Display`, alors elle rejoint le groupe `display`, sans aucun secret.
- Étant donné une connexion de l'interface GM, quand elle s'annonce comme `GameMaster` avec le bon code, alors elle rejoint le groupe `gm` et ses intentions GM sont acceptées. Avec un mauvais code ou sans code, elle reçoit un refus sous forme de code (`GameMasterCodeInvalid`) et n'entre dans aucun groupe.
- Étant donné une connexion non authentifiée comme GM, quand elle envoie une intention GM, alors l'intention est ignorée et journalisée en `Warning`, sans exception renvoyée au client.
- Étant donné un message malformé (champ manquant, valeur hors bornes, rôle inconnu), quand le hub le reçoit, alors il est ignoré et journalisé en `Warning`. Aucune `HubException` ne transmet de message au client.
- Étant donné la journalisation, quand un GM s'authentifie ou échoue, alors le code saisi n'apparaît jamais dans les logs.
- Étant donné `PartyGame.Contracts`, quand on l'inspecte, alors il contient l'interface `IGameClient` (messages du serveur vers les clients), et `npm run generate:contracts` produit son équivalent TypeScript.
- Étant donné le client, quand un composant veut communiquer avec le serveur, alors il passe par `shared/connection` (connexion, envoi d'intention, réception des messages). Une règle ESLint interdit l'import de `@microsoft/signalr` ailleurs.
- Étant donné les tests d'intégration du hub (`WebApplicationFactory` et un vrai client `Microsoft.AspNetCore.SignalR.Client`), quand ils s'exécutent, alors ils couvrent l'annonce de chaque rôle, le refus d'un mauvais code GM et l'ignorance d'un message malformé.

**Comportement en cas d'erreur**
Joueurs et public : un message rejeté ne produit aucun affichage. GM : un code refusé ramène à l'écran de saisie du code (US-E04-01). Opérateur : les entrées malformées sont visibles en `Warning`.

**Notes techniques**
- `Hub<IGameClient>`. Le préfixe `/hub` est déjà réservé dans `ServerPaths` ; ajouter `/hub` au proxy Vite avec `ws: true`, repris par `vite preview` pour les tests E2E.
- L'identité d'un joueur n'est jamais liée au `ConnectionId` : le rôle et, pour un joueur, son `PlayerId` sont conservés dans `Context.Items` après identification (US-E04-02 et US-E05-01). Un même joueur peut avoir plusieurs connexions (deux onglets) : chaque joueur a son propre groupe (`player:<PlayerId>`).
- La vérification du code GM réutilise `GameMasterCode.Verify` (US-E02-05). Pas de limitation du nombre de tentatives (décision 1 du README).
- Sérialisation SignalR alignée sur `ContractJsonOptions` (camelCase, énumérations en chaînes).
- Extension de `PartyGame.TypeGen` pour traduire `IGameClient`, comme prévu par l'[ADR 0003](../../docs/adr/0003-generation-types-typescript.md). Les méthodes du hub appelées par les clients sont décrites du côté TypeScript dans `shared/connection`, à partir des DTO générés.
- `EnableDetailedErrors` uniquement en développement.
- Nouvelles dépendances (décision 3 du README) : `@microsoft/signalr` et `Microsoft.AspNetCore.SignalR.Client` (tests).
- Mettre à jour CLAUDE.md (« Commandes » si le proxy change, « Points d'attention » si utile).
- Réalisation : l'annonce est une méthode unique `Announce(Announcement)`, où `Announcement` porte le rôle et le code GM (nullable, toujours présent sur le fil). Elle répond un `AnnouncementResult` dont le motif de refus (`AnnouncementRefusal`) vaut `GameMasterCodeInvalid` ou `MessageInvalid`. Le rôle `Player` est refusé comme malformé : un joueur s'identifie par `JoinGame` ou `ResumeSession`. Une nouvelle annonce remplace la précédente, y compris quand elle est refusée ; un message malformé ne change rien.
- Réalisation : les méthodes du hub reçoivent leur message en `JsonElement` et le lisent avec `HubMessage.TryRead` (conventions de `ContractJsonOptions`). SignalR rejetterait sinon un message mal typé avant d'atteindre le hub, avec un log `Debug` invisible. Le `Warning` ne contient que le chemin JSON fautif, jamais la valeur. Un nombre d'arguments erroné reste rejeté par SignalR lui-même, sans message transmis au client.
- Réalisation : une intention GM se déclare avec `[GameMasterOnly]` ; le filtre de hub `GameMasterOnlyFilter` l'ignore (réponse vide, log `Warning`) si la connexion n'est pas authentifiée comme GM. `HubExceptionFilter` journalise en `Error` toute exception d'une méthode du hub et répond vide. Aucune intention GM n'existant encore, les tests passent par un hub de test (`ProbeHub`) monté à côté du vrai avec la configuration SignalR du serveur.
- Réalisation : les méthodes du hub portent le suffixe `Async` et gardent leur nom sur le fil par `[HubMethodName]` (`GameHub.Announce`).
- Réalisation : `IGameClient` est vide jusqu'aux snapshots (US-E03-05). `PartyGame.TypeGen` traduit une interface non polymorphe de `Contracts` en interface TypeScript dont chaque méthode garde son nom C# (la cible SignalR) et retourne `void` ; seules les méthodes retournant `Task`, sans surcharge, sont acceptées.
- Réalisation : côté client, `shared/connection/gameHub.ts` expose `createGameConnection` (`start`, `stop`, `invoke` typé par `GameHubMethods`, `on` typé par `IGameClient`). Aucune page ne se connecte encore : la connexion de la TV et du GM arrive avec les snapshots (US-E03-05) et l'écran du code (US-E04-01). Le proxy Vite de `/hub` (`ws: true`) existait déjà. Vérifié avec un client Node en WebSocket seul, sur le serveur .NET et à travers `npm run dev`.

**Hors périmètre**
- Reconnexion automatique et réidentification (E05).
- Idempotence des intentions par `ClientSeq` et file d'envoi côté client (E08).
- Remontée des erreurs clients via `ReportClientError` (E10).
