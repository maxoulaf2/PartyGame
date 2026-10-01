### US-E04-01 — Accès à l'interface GM par le code

**Statut :** À faire

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

**Hors périmètre**
- Limitation du nombre de tentatives (décision 2 du README).
- Déconnexion explicite du GM et plusieurs GM simultanés : rien ne les empêche, mais rien n'est fait pour eux.
