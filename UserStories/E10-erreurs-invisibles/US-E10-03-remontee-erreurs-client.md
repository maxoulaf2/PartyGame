### US-E10-03 — Remontée des erreurs des clients

**Statut :** Prête

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

**Hors périmètre**
- Les `<svelte:boundary>` et l'écran TV jamais vide (US-E10-04).
- Un tableau de bord des erreurs : les logs suffisent.
