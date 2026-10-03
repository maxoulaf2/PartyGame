### US-E10-03 — Remontée des erreurs des clients

**Statut :** Terminée

**En tant que** opérateur
**je veux** retrouver dans les logs du serveur les erreurs JavaScript survenues sur les téléphones, la TV et la console GM
**afin de** corriger des bugs que personne n'a vus à l'écran

**Critères d'acceptation**
- Étant donné une page joueur, TV ou GM, quand une erreur non interceptée survient (`window.onerror`, y compris dans un gestionnaire d'événement) ou qu'une promesse est rejetée sans traitement (`unhandledrejection`), alors l'erreur est envoyée au serveur par `ReportClientError`, et rien ne s'affiche à l'écran.
- Étant donné un rapport reçu, quand le serveur le journalise, alors il écrit un `Warning` structuré : rôle, page, type d'erreur, message, début de la pile, type de vue de manche affichée, version du snapshot affiché, identifiant de build, et joueur s'il est identifié. Les champs sont tronqués à une longueur maximale.
- Étant donné la même erreur répétée en boucle (une erreur dans un rendu, par exemple), quand elle survient, alors le client ne l'envoie qu'une fois par minute (même message et même début de pile), et jamais plus de 10 rapports par minute au total.
- Étant donné un client déconnecté, quand une erreur survient, alors son rapport est mis en attente (au plus 20) et envoyé après la reconnexion.
- Étant donné un client qui envoie trop de rapports (client modifié, boucle), quand le serveur les reçoit, alors il en limite aussi le nombre par connexion, et journalise une seule fois qu'il en ignore.
- Étant donné un rapport malformé, quand le hub le reçoit, alors il est ignoré et journalisé en `Warning`, comme tout message malformé.
- Étant donné l'envoi d'un rapport qui échoue, quand l'échec survient, alors il ne provoque lui-même aucun nouveau rapport ni aucune erreur visible.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent : Vitest (déduplication, limitation, file d'attente pendant une coupure, troncature) et intégration du hub (rapport journalisé avec ses champs, rapport malformé, limitation côté serveur, rapport d'un client non identifié).

**Comportement en cas d'erreur**
Défaut : rien n'est jamais visible, sur aucune page. Le GM n'est pas informé d'une erreur client : il ne peut pas agir. Seules les erreurs de l'écran TV deviennent des incidents GM (US-E10-04).

**Notes techniques**
- Contrat `ClientErrorReport` dans `PartyGame.Contracts` (champs ci-dessus, `kind` : `Error`, `UnhandledRejection`, `RenderFailed`), puis `npm run generate:contracts`. Méthode du hub `ReportClientError`, accessible à toute connexion, même non annoncée : une erreur peut survenir avant l'annonce.
- Côté client, un module `shared/errors/` installe les handlers globaux au démarrage de chaque page, et expose `reportError(kind, error)` pour les `<svelte:boundary>` de US-E10-04. Il passe par `shared/connection` pour l'envoi.
- Le rapport ne contient ni pseudo, ni jeton, ni code GM : seulement le `PlayerId` connu du serveur, qu'il ajoute lui-même.
- Les erreurs ignorées par le navigateur (« ResizeObserver loop… », erreurs de scripts d'extensions sans pile) sont filtrées avant envoi.
- `StaleBuildReport` (US-E05-04) reste à part : ce n'est pas une erreur.
- Réalisation : contrats. `ClientErrorKind` (`Error`, `UnhandledRejection`, `RenderFailed`) et `ClientErrorReport` : rôle de la page (elle peut ne pas l'avoir encore annoncé), chemin de la page sans requête ni fragment, type, message (nom et message de l'erreur, `TypeError: …`), pile ou `null`, type de la vue de manche affichée, version du snapshot affiché et identifiant de build, ces trois derniers à `null` quand ils n'existent pas.
- Réalisation : serveur. `GameHub.ReportClientError` journalise un `Warning` structuré, avec la connexion et le `PlayerId` que le serveur connaît (`null` pour une connexion non identifiée). `ClientErrorFields` tronque chaque champ (page 200, message 500, pile 2 000, type de vue et build 64 caractères), sans couper une paire de substitution. `ClientErrorAllowance`, gardé dans `Context.Items`, admet 20 rapports par minute et par connexion : le double des 10 d'une page, qui renvoie d'un coup les 20 mis en attente pendant une longue coupure. Le premier rapport ignoré est journalisé une seule fois par connexion. Le décompte précède la lecture du message, pour borner aussi un flot de rapports malformés.
- Réalisation : client. `shared/errors/` : `errorReport.ts` (description d'une valeur quelconque, troncature, filtrage du bruit : « ResizeObserver loop », « Script error. » sans pile, scripts d'extensions d'après le fichier mis en cause), `errorReporter.ts` (`ErrorReporter` : déduplication par message et début de pile pendant une minute, 10 rapports par minute au plus, file de 20 rapports gardant les premiers, renvoi à chaque connexion établie, envoi en échec remis en file sans nouveau rapport), `uncaughtErrors.ts` (écoute de `error` et `unhandledrejection`) et `errorReporting.ts`, le point d'entrée des pages : `startErrorReporting(role)` dans chaque `main.ts`, avant le montage, `connectErrorReporting(connection, game)` dans chaque `App.svelte`, avant le démarrage de la connexion, et `reportError(kind, error)` pour les `<svelte:boundary>` de US-E10-04. Le contexte (version, type de vue) est lu au moment de l'erreur, pas de l'envoi.
- Réalisation : tests. `ReportClientErrorTests` (champs journalisés, joueur identifié, troncature, état inchangé, messages malformés, limitation par connexion et sa fenêtre), `ClientErrorAllowanceTests`, `ClientErrorFieldsTests`. Vitest : `errorReport.test.ts`, `errorReporter.test.ts`, `uncaughtErrors.test.ts`. Playwright : `errorReports.spec.ts`, sur les trois pages et les trois navigateurs, vérifie qu'une erreur d'un gestionnaire et une promesse rejetée partent vers le serveur sans rien changer à l'écran.

**Hors périmètre**
- Les `<svelte:boundary>` et l'écran TV jamais vide (US-E10-04).
- Un tableau de bord des erreurs : les logs suffisent.
