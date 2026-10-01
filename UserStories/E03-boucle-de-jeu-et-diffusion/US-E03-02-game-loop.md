### US-E03-02 — `GameLoop` : une file et un seul écrivain

**Statut :** Terminée

**Résultat attendu**
Dans `PartyGame.Server`, une boucle unique traite une à une les entrées de la partie, déposées dans un `Channel<GameInput>`. Elle est le seul écrivain de l'état, exécute les effets, signale les changements d'état à la diffusion, et survit à toute exception.

**Critères d'acceptation**
- Étant donné le serveur démarré, quand il est prêt, alors une `GameLoop` tourne dans un `BackgroundService` avec une partie dans son état initial.
- Étant donné des entrées déposées en parallèle depuis plusieurs threads, quand la boucle les traite, alors elle les applique une par une, dans l'ordre de la file, sans `lock` ni collection concurrente dans le moteur.
- Étant donné une entrée qui change l'état, quand la boucle l'a traitée, alors elle remplace l'état courant, exécute les effets de la transition, puis notifie la diffusion (consommée par US-E03-05).
- Étant donné une entrée rejetée (même instance d'état retournée), quand la boucle l'a traitée, alors rien n'est diffusé et un log `Debug` est écrit.
- Étant donné un moteur qui lève une exception sur une entrée, quand la boucle la traite, alors l'état précédent est conservé, un log `Error` est écrit avec le type de l'entrée, et la boucle continue de traiter les entrées suivantes.
- Étant donné l'arrêt de l'application, quand il est demandé, alors la boucle s'arrête proprement (`CancellationToken` propagé, file fermée), sans exception non gérée.
- Étant donné `GameContext`, quand la boucle le crée, alors `Now` provient du `TimeProvider` injecté, et non de `DateTime.UtcNow`.
- Étant donné un test avec `FakeTimeProvider` et un moteur de test qui lève une exception, quand il s'exécute, alors il vérifie que la boucle reste vivante et que l'état est inchangé.

**Comportement en cas d'erreur**
Joueurs et public : rien n'est visible, le dernier snapshot reste affiché. GM : rien pour l'instant ; l'incident GM et la proposition de passer l'étape arrivent en E10. Opérateur : le log `Error` contient la pile d'appels.

**Notes techniques**
- Une seule partie par serveur. La boucle et son état vivent dans un singleton ; la file est exposée aux producteurs (hub, timers) par une abstraction d'écriture seule, par exemple `IGameInputWriter`.
- La file est non bornée, ou bornée avec une capacité large et une attente côté producteur : une intention ne doit jamais être perdue silencieusement.
- Certaines entrées attendent une réponse (inscription d'un joueur, US-E04-02) : l'entrée peut porter un `TaskCompletionSource` que la boucle complète après le traitement. Le hub l'attend, sans jamais toucher à l'état.
- L'exécution de chaque effet est protégée séparément : un effet qui échoue n'empêche ni les autres effets ni la diffusion.
- Le traitement d'une entrée doit rester rapide : aucune entrée/sortie dans la boucle hors des effets.
- Messages de log structurés, sans interpolation.

**Hors périmètre**
- Incidents GM et proposition de passer une manche qui échoue de façon répétée (E10).
- Persistance de l'état après chaque transition (E11).
- Plusieurs parties simultanées.
