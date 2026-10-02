### US-E05-01 — Reconnexion automatique par jeton

**Statut :** Terminée

**En tant que** joueur
**je veux** retrouver la partie tout seul après avoir verrouillé mon téléphone, changé d'application ou rechargé la page
**afin de** ne jamais avoir à me réinscrire ni à chercher comment revenir

**Critères d'acceptation**
- Étant donné un joueur inscrit, quand il recharge la page, alors le téléphone présente son jeton au serveur (`ResumeSession(token)`) et affiche le lobby avec son pseudo, sans passer par l'écran d'inscription.
- Étant donné un téléphone mis en veille une minute, quand il est réveillé et la page de nouveau au premier plan, alors il se reconnecte, se réidentifie et affiche l'état courant sans aucune action du joueur. Vérifié sur un iPhone (Safari) et un Android (Chrome).
- Étant donné une coupure Wi-Fi de plusieurs minutes, quand le réseau revient, alors le client se reconnecte de lui-même : les tentatives ne s'arrêtent jamais (décision 1 du README).
- Étant donné un joueur reconnecté, quand sa réidentification est acceptée, alors il reçoit aussitôt le snapshot courant, et la TV et le GM le voient de nouveau connecté.
- Étant donné un jeton inconnu du serveur (serveur redémarré, `localStorage` d'une ancienne soirée), quand le téléphone le présente, alors il reçoit un code (`SessionUnknown`), oublie le jeton et affiche l'écran d'inscription prérempli avec le dernier pseudo utilisé.
- Étant donné un joueur ouvert dans deux onglets, quand l'un des deux se ferme, alors le joueur reste connecté tant que l'autre l'est.
- Étant donné l'écran TV et l'interface GM, quand ils perdent puis retrouvent la connexion, alors ils s'annoncent de nouveau (avec le code mémorisé pour le GM) et reçoivent le snapshot courant.
- Étant donné les tests d'intégration du hub, quand ils s'exécutent, alors ils couvrent la reprise par jeton, le jeton inconnu et la présence avec deux connexions. Un test E2E Playwright coupe le réseau d'un contexte de navigateur (`context.setOffline`) puis le rétablit.

**Comportement en cas d'erreur**
Joueur : pendant la coupure, le dernier affichage reste en place, verrouillé (US-E05-02). Public : la TV conserve son affichage. GM : il voit le joueur déconnecté puis reconnecté, sans autre alerte.

**Notes techniques**
- Le module `shared/connection` gère le cycle complet : `withAutomaticReconnect` avec une politique sans limite, réannonce du rôle dans `onreconnected`, et redémarrage complet de la connexion si `onclose` survient malgré tout. Un `visibilitychange` vers `visible` déclenche une tentative immédiate si la connexion n'est pas établie : Safari iOS suspend les WebSockets en arrière-plan.
- L'identité n'est jamais liée au `ConnectionId` : la réidentification associe la nouvelle connexion au `PlayerId` du jeton, dans `Context.Items` et dans le groupe du joueur.
- La présence compte les connexions par joueur. Ce compteur vit dans le hub, hors de l'état ; seules les transitions connecté → déconnecté et inverse sont déposées dans la file (`PlayerConnectionLost`, `PlayerConnectionRestored`).
- Les jetons ne survivent pas à un redémarrage du serveur avant E11 : ce cas est couvert par le code `SessionUnknown`.
- La resynchronisation d'horloge à la reconnexion est traitée par US-E05-03.
- Réalisation : contrats `ResumeSessionRequest(token)`, `ResumeSessionResult(refusal, playerId)` et codes `ResumeSessionRefusal` : `SessionUnknown`, `AlreadyIdentified` (connexion déjà inscrite ou identifiée), `ResumeFailed` (connexion fermée pendant la reprise), `MessageInvalid`.
- Réalisation : le hub expose `ResumeSession`. Il cherche le jeton dans `GameState.PlayerTokens` sans passer par la boucle (les jetons ne sont jamais retirés et l'état lu est immuable), place la connexion dans le groupe du joueur, l'associe au `PlayerId` dans `Context.Items`, puis envoie le snapshot courant à l'appelant. Logs : `Information` « Player {PlayerId} resumed their session on connection {ConnectionId} », `Debug` pour un jeton inconnu ; le jeton n'est jamais journalisé.
- Réalisation : présence. `PlayerConnections` compte les connexions de chaque joueur et dépose lui-même `PlayerConnectionRestored` (première connexion d'un joueur qui n'en avait plus) et `PlayerConnectionLost` (dernière connexion fermée), sous le même verrou que le compteur : un onglet qui se ferme pendant qu'un autre reprend la session ne peut pas inverser les deux entrées dans la file. `IGameInputWriter.WriteAsync` garantit pour cela qu'une entrée est en file au retour de l'appel. Le moteur traite `PlayerConnectionRestored` dans `Lobby/Presence` (refus `PlayerUnknown` et `PlayerAlreadyConnected`), dans toutes les phases.
- Réalisation : côté client, `createGameConnection` (`shared/connection/gameHub.ts`) ne renonce jamais : `withAutomaticReconnect` avec les délais 0, 1, 2, 5 puis 10 s indéfiniment (`reconnectDelay`), redémarrage complet avec les mêmes délais si la connexion se ferme malgré tout, et premier démarrage réessayé jusqu'au succès. `visibilitychange` vers `visible` et l'événement `online` déclenchent une tentative immédiate : fin de l'attente en cours, ou arrêt de la reconnexion de SignalR, dont on ne peut pas raccourcir le délai, pour la relancer aussitôt. `GameConnection` n'expose plus que `onReconnecting` (connexion perdue) et `onReconnected` (nouvelle connexion établie) ; l'écran TV et le GM s'y réannoncent comme avant.
- Réalisation : `PlayerSession` a un statut `registering`, `resuming` ou `joined`. Avec un jeton conservé, la page joueur affiche l'écran d'attente neutre (« Retour dans la partie… ») au lieu du formulaire, présente le jeton à chaque connexion, et passe au lobby dès l'acceptation. `SessionUnknown` efface le jeton et affiche le formulaire prérempli avec le dernier pseudo ; tout autre refus, ou une coupure pendant la reprise, conserve le jeton pour la connexion suivante.
- Réalisation : tests du moteur (`PresenceTests` : reconnexion d'un seul joueur, après le lancement, refus si déjà connecté ou inconnu), du serveur (`PlayerConnectionsTests` : signalements de présence, deux connexions, entrées toujours alternées sous forte concurrence ; `ResumeSessionTests` : reprise après déconnexion avec mise à jour de la TV et du GM, reprise après le lancement, deux onglets, reprises successives, jeton inconnu puis inscription sur la même connexion, connexion déjà identifiée, messages malformés, jeton absent des snapshots et des logs), Vitest (`gameHub.test.ts` : délais, démarrage réessayé, redémarrage après une longue coupure, tentative immédiate au réveil, arrêt ; `playerSession.test.ts` : reprise au démarrage, à chaque reconnexion, jeton inconnu, coupure et échec pendant la reprise) et Playwright (`e2e/reconnection.spec.ts`, sur iPhone et Pixel : rechargement sans formulaire, coupure réseau puis retour visible sur la TV, jeton inconnu). La coupure combine `context.setOffline` et la fermeture des WebSockets relayés par `page.routeWebSocket` : sous WebKit, l'émulation hors ligne seule laisse ouverte une WebSocket existante.
- Réalisation : vérifié sur de vrais appareils, sur le serveur .NET servant le front construit : téléphone mis en veille puis réveillé, revenu dans le lobby avec son pseudo sans aucune action, et de nouveau affiché connecté sur la TV.

**Hors périmètre**
- Renvoi des intentions non acquittées (E08).
- Reprise de la partie après un crash du serveur (E11).
- Maintien de l'écran allumé (Wake Lock indisponible en HTTP simple).
