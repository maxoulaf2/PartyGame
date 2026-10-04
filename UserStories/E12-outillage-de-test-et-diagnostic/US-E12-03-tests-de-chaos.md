### US-E12-03 — Tests de chaos

**Statut :** Terminée

Des tests automatisés provoquent les pannes que la phase 3 doit rendre invisibles : coupure d'un téléphone en pleine manche, exception injectée dans un mode, image manquante, serveur tué en pleine question. Ils vérifient que les joueurs et le public ne voient rien et que seul le GM est informé. Ils automatisent le critère de sortie de la phase.

**Critères d'acceptation**
- Étant donné une table complète en pleine question (US-E12-02), quand un téléphone perd le réseau, que son joueur choisit une proposition hors ligne puis que le réseau revient, alors le choix s'affiche « en attente » pendant la coupure, la réponse est comptée une seule fois après la reconnexion, et la console GM montre le joueur déconnecté puis reconnecté, sans autre message. La TV, qui n'affiche pas l'état des connexions pendant une question, ne montre que le compteur de réponses.
- Étant donné un serveur dont l'injection de pannes est activée, quand une entrée de la manche lève l'exception injectée, alors les téléphones et la TV ne reçoivent aucun nouveau snapshot et n'affichent rien de particulier, et la console GM montre l'incident. À la troisième exception, elle propose de passer la manche ; une fois passée, la manche suivante se joue normalement.
- Étant donné une question dont l'image manque sur le disque au moment de son affichage, quand la TV l'affiche, alors la mise en page reste propre sans l'image, et la console GM montre l'incident `DisplayMediaFailed`.
- Étant donné une table complète en pleine question, un joueur ayant déjà répondu, quand le serveur dédié est tué puis relancé sur le même dossier de données, et que le GM saisit le code et reprend la partie, alors les téléphones et la TV reviennent dans la question sans aucune action, avec le temps qui restait. Le joueur qui avait répondu voit sa réponse ; les autres peuvent répondre ; à la révélation, les points sont ceux attendus.
- Étant donné le même scénario de redémarrage avec dix bots (US-E12-01) en plus des trois joueurs, quand le serveur est tué pendant une rafale de réponses, alors aucune réponse acquittée n'est perdue et aucune n'est comptée deux fois.
- Étant donné le serveur tué pendant l'écriture du fichier de partie (test d'intégration, écriture interrompue simulée), quand il redémarre, alors il propose le dernier état complet écrit, jamais un fichier tronqué.
- Étant donné l'injection de pannes, quand le serveur tourne en environnement `Production`, alors elle est inactive quelle que soit la configuration, et un `Warning` le signale au démarrage si elle était demandée.
- Étant donné `npm run e2e` et `dotnet test`, quand ils s'exécutent, alors tous ces scénarios en font partie.

**Comportement en cas d'erreur**
C'est l'objet même de l'US : joueurs et public ne voient rien, le GM voit les incidents et peut passer la manche.

**Notes techniques**
- Les scénarios E2E (coupure, image manquante, redémarrage, exception vue dans la console GM) utilisent la table et le serveur dédié de US-E12-02. La coupure réutilise la technique de `e2e/reconnection.spec.ts` : `context.setOffline` et fermeture des WebSockets par `page.routeWebSocket`, nécessaire sous WebKit.
- Injection de pannes (confirmée au début de l'US) : une section de configuration `FaultInjection` (par exemple `FaultInjection:FailOnInput=PlayerRoundInput`, `FaultInjection:FailCount=3`), lue seulement hors de l'environnement `Production`, qui enveloppe le moteur d'un décorateur levant l'exception sur l'entrée désignée. Elle sert aussi à la vérification manuelle du critère de sortie sur de vrais appareils. Option écartée : un mode de test enregistré seulement dans les tests d'intégration (comme `TestQuizMode`), sans aucun code d'injection dans le serveur, mais sans possibilité de vérification en E2E ni sur de vrais appareils.
- Les tests d'intégration (exception injectée, bots, écriture interrompue) utilisent `WebApplicationFactory` et `LeakAssert.NoSecretReceived` pour vérifier que l'incident n'atteint ni les joueurs ni la TV.
- L'image manquante se provoque en supprimant le fichier d'une copie du pack de test, après le lancement de la partie.

- Décision prise pendant l'US : une réponse est acquittée une fois enregistrée. `SendRoundIntent` répondait dès que la boucle avait traité l'intention, avant l'écriture asynchrone : un serveur tué dans les millisecondes suivantes perdait une réponse acquittée, que le téléphone ne renvoyait jamais. Il attend désormais `GamePersistence.WaitUntilSavedAsync` ; la boucle n'attend toujours pas le disque. Amendement de l'[ADR 0005](../../docs/adr/0005-enregistrement-de-la-partie.md).
- Bug trouvé par le scénario de redémarrage : à la reprise, la boucle diffusait et enregistrait l'état de `ResumeSavedGame`, avec les échéances d'avant l'arrêt, avant celui de `GameResumed` ; la TV affichait un instant un compte à rebours amputé du temps d'arrêt. Cet état intermédiaire n'est plus notifié.
- Réalisation : serveur. `PartyGame.Server/Faults` : `FaultInjectionOptions`, `FaultInjectingEngine` (décorateur de `IGameEngine`), `AddFaultInjection` (ignorée en `Production`, nom d'entrée inconnu : arrêt au démarrage avec la liste des entrées) et `Warning` au démarrage, que l'injection soit active ou ignorée.
- Réalisation : tests .NET. `FaultInjectionTests` (trois échecs : incident GM seul, manche proposée puis passée, manche suivante normale, `LeakAssert` sur la TV et un téléphone ; `Production` ; entrée inconnue) ; `ServerKilledTests` dans `PartyGame.Bots.Tests` (vrai processus tué au milieu des réponses de 13 bots, puis relancé avec un autre code : aucune réponse acquittée perdue, aucune comptée deux fois) ; `PersistenceHostingTests.SendRoundIntent_Acknowledged_TheAnswerIsSavedAlready` ; `ResumeOfferTests.Startup_KilledWhileWritingTheSave_OffersTheLastCompleteSave` ; `GamePersistenceTests.WaitUntilSaved_*` ; `TransparentResumeTests` vérifie qu'aucun écran ne reçoit l'ancienne échéance.
- Réalisation : E2E. `e2e/chaos.spec.ts` sur un pack à part (`e2e/chaos-packs`, une question illustrée) : coupure silencieuse d'un téléphone (`RelayedNetwork.drop`, puis `cut` pour le retour du réseau), image supprimée après le lancement, serveur tué puis relancé avec un nouveau code saisi par le GM, injection de pannes. La fixture `table` gagne les options `serverSettings` et `serverPacks`, `start(settings)` et le relais des WebSockets de chaque téléphone ; le relais de `reconnection.spec.ts` passe dans `e2e/network.ts`.

**Hors périmètre**
- Le test de charge sur le Raspberry Pi (E21).
- Les incidents audio (E14).
