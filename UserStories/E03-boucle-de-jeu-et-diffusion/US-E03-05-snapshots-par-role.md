### US-E03-05 — Snapshots versionnés, projetés par rôle et diffusés

**Statut :** À faire

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
- La diffusion est un effet exécuté par la boucle après la transition, protégée comme les autres effets.
- Le contenu des snapshots du lobby (joueurs, adresse, phase) est défini par E04. Cette US peut démarrer avec un contenu minimal (phase et nombre de joueurs).
- Le helper de test de non-fuite réutilisable par tous les modes est l'objet de E07 ; ici, des tests ciblés suffisent.

**Hors périmètre**
- Diffs entre snapshots (écartés par l'ADR 0001).
- Projections propres à un mode (E07, E08).
