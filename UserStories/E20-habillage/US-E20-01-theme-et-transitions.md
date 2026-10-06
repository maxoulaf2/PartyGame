### US-E20-01 — Thème visuel et transitions de l'écran TV

**Statut :** Terminée

**En tant que** public
**je veux** un écran TV au style soigné, qui passe d'un écran à l'autre sans à-coups
**afin de** me sentir dans un jeu télévisé plutôt que devant une page web

**Critères d'acceptation**
- Étant donné les pages TV, joueur et GM, quand elles s'affichent, alors elles utilisent la police auto-hébergée et les variables du thème (`shared/theme.css`) ; aucune couleur ni police n'est écrite en dur dans un composant.
- Étant donné la TV, quand elle passe d'un écran à l'autre (lobby, introduction de manche, question, révélation, classement, podium, pause), alors l'écran sortant s'efface et l'entrant apparaît par une transition de moins de 600 ms, sans écran vide entre les deux.
- Étant donné la TV, quand elle affiche une révélation (bonne réponse au quiz, titre au blind test, bonnes réponses aux questions ouvertes), alors la réponse est mise en valeur par une animation brève.
- Étant donné la TV en 1080p sur un Raspberry Pi 4 sous Chromium, quand une transition se joue avec 20 joueurs inscrits, alors elle reste fluide, sans saccade visible.
- Étant donné un navigateur réglé sur `prefers-reduced-motion`, quand la TV change d'écran, alors elle passe d'un écran à l'autre sans mouvement, par un simple fondu.
- Étant donné une page chargée, quand on inspecte ses requêtes, alors aucune ne sort du réseau local : la police est servie par le serveur.
- Étant donné les tests, quand ils s'exécutent, alors un test E2E vérifie qu'aucune requête externe n'est émise par les trois pages, et que la police est bien chargée sur la TV.

**Comportement en cas d'erreur**
Défaut : si la police ne se charge pas, la pile de polices système prend le relais ; une transition qui échoue laisse l'écran entrant affiché.

**Notes techniques**
- Police au format `woff2` dans `client/src/assets/fonts/`, sous licence OFL, avec sa licence copiée à côté ; un ou deux styles seulement pour limiter le poids. `font-display: swap`.
- Transitions entre écrans avec les transitions de Svelte (`svelte/transition`), en `opacity` et `transform` uniquement ; aucune bibliothèque d'animation.
- Les vues des modes gardent leur contenu ; seul le conteneur de chaque page orchestre les transitions entre écrans, sans connaître les modes.
- Réalisation : police. Nunito variable (graisses 200 à 1 000 en un seul fichier, sous-ensemble latin, 39 Ko) dans `client/src/assets/fonts/`, avec sa licence `OFL.txt`, déclarée par `@font-face` dans `shared/theme.css` en tête de `--font-family` ; Vite l'émet sous `/assets`. Les dernières couleurs et polices en dur passent par le thème : `--color-backdrop`, `--color-white` et `--color-black` (QR code, flash du diagnostic), `--font-family-mono`.
- Réalisation : transitions. `display/App.svelte` enveloppe l'écran courant dans un `{#key}` dont la clé change avec l'écran (lobby, introduction, manche, classement, podium, aperçu, attente) ; les écrans sortant et entrant, superposés dans la même cellule de grille, se croisent en 450 ms (`display/screenChange.ts` : fondu et léger zoom jamais au-delà de la taille naturelle, pour ne rien pousser dans la marge que les TV rognent ; fondu seul sous `prefers-reduced-motion`). L'écran sortant garde son contenu et devient `inert` et `aria-hidden`. La pause apparaît et disparaît en fondu. Les phases d'une manche (question, révélation) restent dans la vue du mode.
- Réalisation : révélation. L'animation `reveal-highlight` de `display/display.css` (la réponse se pose, rebondit une fois et se stabilise, sans dépasser sa taille) est appliquée à la bonne proposition du quiz, au titre du blind test, à la réponse attendue et aux réponses acceptées des questions ouvertes, sauf sous `prefers-reduced-motion`.
- Réalisation : tests. `e2e/display.spec.ts` vérifie le chargement de la police depuis le serveur sans requête externe, et le fondu enchaîné sans écran vide ; les tests existants vérifient l'absence de requête externe sur les quatre pages.
- Révision : habillage « pop party » (maquettes Claude Design, 2026-10-06). Les pages joueur et TV chargent `shared/pop.css` après `shared/theme.css` : fond violet électrique, cartes crème en « stickers » (contour encre, ombre portée franche), couleurs vives, confettis (`shared/components/Confetti.svelte`), logo en deux stickers (`Logo.svelte`), police Bricolage Grotesque variable (sous-ensemble latin, 77 Ko, licence `OFL-bricolage-grotesque.txt`). La console GM et la page de diagnostic gardent le thème sombre et Nunito. Sur le téléphone, le verdict du quiz colore tout l'écran (vert pour une bonne réponse, rose pour une erreur) et le compte à rebours devient un anneau qui se vide (`Countdown` avec `ring`). Sur la TV, les tailles reprennent les maquettes en 960 × 540 via `--u` (un pixel de maquette) ; avec beaucoup de joueurs ou de longs textes, les écrans passent à des stickers plus fins et aux tailles d'avant, pour que les tests de 20 pseudos longs tiennent toujours.

**Hors périmètre**
- Plusieurs thèmes au choix, ou un thème fourni par le pack.
- Les animations des classements, déjà couvertes par US-E18-02.
