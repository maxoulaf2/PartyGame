### US-E20-03 — Vérification de l'accessibilité

**Statut :** À faire

**En tant que** joueur daltonien, malvoyant ou assis au fond de la pièce
**je veux** que chaque information soit lisible et compréhensible sans dépendre de la couleur
**afin de** jouer à égalité avec les autres

**Critères d'acceptation**
- Étant donné les variables de couleur du thème, quand le test de contraste s'exécute, alors chaque couple texte/fond utilisé atteint un ratio d'au moins 4,5:1 (WCAG AA), et d'au moins 7:1 pour les textes de la TV.
- Étant donné chaque écran de chaque page (TV, joueur, GM) et de chaque mode, quand il est passé en revue, alors aucune information n'est portée par la couleur seule : propositions, bonne et mauvaise réponse, évolution des rangs, connexion d'un joueur, incidents.
- Étant donné un téléphone dont le texte est agrandi à 200 % dans les réglages du système, quand la page joueur s'affiche, alors tous ses éléments restent visibles et utilisables, sans texte coupé.
- Étant donné les zones tactiles de la page joueur, quand on les mesure, alors chacune fait au moins 48 px de côté.
- Étant donné la revue, quand elle est terminée, alors ses résultats et les corrections faites sont consignés dans `docs/accessibilite.md`, avec une liste de contrôle à reprendre pour tout nouveau mode.
- Étant donné les tests, quand ils s'exécutent, alors un test Vitest vérifie les contrastes du thème, et un test E2E vérifie la taille des zones tactiles de la page joueur pour chaque mode.

**Comportement en cas d'erreur**
Sans objet.

**Notes techniques**
- Le test de contraste lit `shared/theme.css`, calcule les ratios WCAG et nomme le couple fautif ; pas de nouvelle dépendance (décision 5 du README).
- Le simulateur de daltonisme des outils de développement de Chrome sert à la revue manuelle.
- La liste de contrôle de `docs/accessibilite.md` est référencée par `docs/coding-guidelines.md`.

**Hors périmètre**
- Le lecteur d'écran sur l'écran TV.
- Un audit automatique complet (axe-core), qui demanderait une nouvelle dépendance.
