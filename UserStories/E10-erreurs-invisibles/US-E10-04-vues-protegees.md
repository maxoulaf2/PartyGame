### US-E10-04 — Vues protégées et écran TV jamais vide

**Statut :** À faire

**En tant que** public
**je veux** que la TV et les téléphones affichent toujours un écran propre, même si une vue plante ou qu'une image manque
**afin de** ne jamais voir d'écran blanc ni de message technique pendant la soirée

**Critères d'acceptation**
- Étant donné une vue de mode (joueur, TV ou GM), quand son rendu lève une exception, alors elle est remplacée par l'écran d'attente neutre de sa page, l'erreur est remontée au serveur (`RenderFailed`, US-E10-03), et le reste de la page (indicateur de connexion, en-tête) continue de fonctionner.
- Étant donné une vue remplacée par l'écran d'attente, quand un snapshot plus récent arrive, alors la page tente de nouveau d'afficher la vue : une erreur passagère se résorbe d'elle-même.
- Étant donné une erreur de rendu hors des vues de mode (lobby, classement, console GM), quand elle survient, alors une protection au niveau de la page affiche l'écran d'attente neutre, puis retente au snapshot suivant.
- Étant donné un téléphone sur l'écran d'attente neutre, quand il l'affiche, alors aucun élément interactif n'est proposé, comme pendant une resynchronisation.
- Étant donné la console GM dont la vue de manche plante, quand elle s'affiche, alors elle montre à la place un message en français (« Affichage de la manche indisponible »), sans détail technique, et garde ses autres contrôles : liste des joueurs, panneau d'incidents, « Passer la manche » s'il est proposé.
- Étant donné l'écran TV, quand il démarre sans avoir encore reçu de snapshot, quand sa vue de mode plante, ou quand la connexion est perdue, alors il n'est jamais vide : écran d'attente habillé (titre du jeu, « La partie continue… ») ou dernier affichage conservé.
- Étant donné une vue de manche qui plante sur l'écran TV, quand l'erreur est remontée, alors le serveur signale un incident GM `DisplayViewFailed` : le GM peut décider de passer la manche.
- Étant donné une image de question introuvable ou illisible sur la TV, quand son chargement échoue, alors la mise en page reste propre sans l'image, et un incident GM `DisplayMediaFailed` est signalé, avec la manche et la question concernées.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent : Vitest (vue qui lève une exception remplacée par l'écran neutre, erreur remontée, nouvelle tentative au snapshot suivant, sur chacune des trois pages), intégration du hub (`RenderFailed` venant de la TV → incident, venant d'un joueur → log seulement ; échec d'image → incident), E2E de l'image manquante dans US-E12-03.

**Comportement en cas d'erreur**
Joueurs et public : l'écran d'attente neutre ou le dernier affichage, jamais un écran vide ni un message technique. GM : un message neutre à la place de la vue de manche, et les incidents de la TV dans son panneau.

**Notes techniques**
- Chaque vue de mode est rendue dans un `<svelte:boundary>` dont le snippet `failed` affiche l'écran d'attente neutre (`shared/components/WaitingScreen.svelte`), et dont `onerror` appelle `reportError` (US-E10-03). La fonction `reset` du boundary est appelée quand la version du snapshot change. Le `{#key}` existant sur l'identifiant de la manche (US-E07-02) reste en place.
- Le même composant de protection enveloppe le contenu de chaque `App.svelte`, pour les erreurs hors des modes. Les `<svelte:boundary>` ne captent pas les erreurs des gestionnaires d'événements ni du code asynchrone : les handlers globaux de US-E10-03 s'en chargent.
- Un incident est tiré d'un rapport client seulement s'il vient d'une connexion annoncée comme `Display`. Les erreurs de rendu côté joueur restent des logs : le GM ne peut pas agir pour un téléphone.
- L'échec d'image passe par une intention dédiée de la TV, `ReportDisplayMediaFailure(mediaId)`, que le serveur traduit en manche et question : la TV ne connaît que l'identifiant du média. E14 la réutilisera pour l'audio.
- Les tests de composants utilisent `mount` de Svelte dans Vitest, sans dépendance ajoutée.

**Hors périmètre**
- L'audio de l'écran TV (E14).
- Une vue de repli propre à chaque mode : l'écran neutre commun suffit.
