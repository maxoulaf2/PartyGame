### US-E04-01 — Accès à l'interface GM par le code

**Statut :** Terminée

**En tant que** game master
**je veux** saisir une seule fois le code affiché dans la console pour accéder aux commandes de la partie
**afin de** piloter la soirée depuis mon téléphone ou ma tablette sans que les autres personnes du réseau le puissent

**Critères d'acceptation**
- Étant donné l'interface GM ouverte sur `/gm/` pour la première fois, quand la page se charge, alors elle affiche un écran de saisie du code : un champ numérique à 6 chiffres (clavier numérique sur mobile) et un bouton de validation.
- Étant donné le bon code saisi, quand le GM valide, alors l'interface affiche la console du lobby, alimentée par la projection `GameMaster`.
- Étant donné un mauvais code, quand le GM valide, alors un message neutre (« Code incorrect ») apparaît sous le champ, qui est vidé et reprend le focus.
- Étant donné un code accepté, quand le GM recharge la page ou que la connexion revient après une coupure, alors il retrouve directement la console, sans ressaisir le code (décision 2 du README).
- Étant donné un code mémorisé qui n'est plus valide (serveur redémarré), quand l'interface se connecte, alors l'écran de saisie réapparaît, avec un message qui invite à relire le code dans la console du serveur.
- Étant donné l'interface GM avant authentification, quand on inspecte les messages WebSocket reçus, alors aucun snapshot `GameMaster` n'a été reçu.
- Étant donné un test E2E Playwright avec un code fixé par `GameMaster:Code`, quand il s'exécute, alors il couvre la saisie d'un mauvais code, puis du bon, puis un rechargement.

**Comportement en cas d'erreur**
GM : serveur injoignable pendant la saisie, le bouton reste désactivé et l'indicateur de reconnexion (US-E05-02) s'affiche ; aucun message technique. Joueurs et public : sans objet.

**Notes techniques**
- L'annonce `GameMaster` avec code est fournie par le hub (US-E03-04) ; cette US livre l'écran, la mémorisation et la réannonce à chaque connexion. Appel : `connection.invoke('Announce', { role: 'GameMaster', gameMasterCode })`, qui répond `{ refusal: null }` ou `{ refusal: 'GameMasterCodeInvalid' }`.
- Les intentions GM à venir (renommage, lancement) portent `[GameMasterOnly]` côté hub : sans authentification, elles sont ignorées.
- Le code est conservé dans le `localStorage` sous une clé propre à l'interface GM. Le `localStorage` est disponible en HTTP simple.
- Le champ accepte les espaces autour (la vérification les tolère) et refuse la validation tant qu'il ne contient pas 6 chiffres.
- Tous les textes sont dans `fr.ts`. Les erreurs arrivent du serveur sous forme de codes (`GameMasterCodeInvalid`) traduits par le client.
- Zones tactiles d'au moins 48 px et `touch-action: manipulation` : le GM pilote souvent depuis un téléphone.
- Réalisation : `GameMasterSession` (`shared/connection/gameMasterSession.svelte.ts`) porte l'authentification : état d'accès (`codeRequired`, `checking`, `granted`), motif du refus (`invalid` pour un code saisi, `expired` pour un code mémorisé refusé) et état de la connexion. Un code mémorisé est présenté à chaque connexion et à chaque reconnexion, sans afficher le formulaire ; un code mémorisé refusé est oublié. Un code saisi n'est mémorisé qu'une fois accepté.
- Réalisation : le code est conservé sous la clé `partygame.gm.code` par `localCodeStorage` (`shared/connection/codeStorage.ts`), qui se comporte comme un stockage vide si le navigateur refuse le `localStorage` (navigation privée).
- Réalisation : l'interface GM (`gm/App.svelte`) affiche le formulaire (`CodeForm.svelte`), l'écran d'attente pendant la vérification d'un code mémorisé, ou la console du lobby (`LobbyConsole.svelte`, nombre de joueurs inscrits, enrichie par US-E04-04). Le champ est un `type="text"` avec `inputmode="numeric"` : les zéros en tête et les espaces survivent. Le bouton reste désactivé tant que le serveur est injoignable ou que le champ ne contient pas 6 chiffres.
- Réalisation : `GameConnection` expose `onReconnecting`, `onReconnected` et `onClose`, et la connexion utilise `withAutomaticReconnect` avec la politique par défaut. Le GM et l'écran TV se réannoncent dans `onReconnected`. La politique sans limite, le redémarrage après `onclose` et `visibilitychange` restent l'objet de US-E05-01 ; l'indicateur de coupure, de US-E05-02.
- Réalisation : les tests E2E démarrent désormais le vrai serveur .NET derrière `vite preview` (port 5199, `GameMaster:Code` fixé dans `e2e/gameServer.ts`). `e2e/gm.spec.ts` couvre mauvais code, bon code, rechargement, code mémorisé expiré et l'absence de snapshot `GameMaster` avant authentification (messages WebSocket inspectés). Il s'exécute aussi sur iPhone (WebKit) et Pixel (Chromium).
- Réalisation : vérifié sur un vrai téléphone, sur le serveur .NET servant le front construit : saisie du code, accès à la console. Le serveur .NET sert le dernier `npm run build` : après une modification du front, le reconstruire avant de tester sur téléphone.

**Hors périmètre**
- Limitation du nombre de tentatives (décision 2 du README).
- Déconnexion explicite du GM et plusieurs GM simultanés : rien ne les empêche, mais rien n'est fait pour eux.
