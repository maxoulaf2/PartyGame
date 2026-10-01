### US-E05-01 — Reconnexion automatique par jeton

**Statut :** À faire

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

**Hors périmètre**
- Renvoi des intentions non acquittées (E08).
- Reprise de la partie après un crash du serveur (E11).
- Maintien de l'écran allumé (Wake Lock indisponible en HTTP simple).
