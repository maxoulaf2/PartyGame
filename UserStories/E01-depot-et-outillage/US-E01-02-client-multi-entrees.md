### US-E01-02 — Client Vite + Svelte 5 multi-entrées

**Statut :** Terminée

**Résultat attendu**
Une application front unique dans `client/`, avec trois points d'entrée (`player`, `display`, `gm`) qui affichent chacun une page d'attente, et un outillage de qualité (typecheck, lint, format, tests unitaires, tests E2E) prêt à l'emploi.

**Critères d'acceptation**
- Étant donné un clone frais, quand on lance `npm install` puis `npm run dev` depuis `client/`, alors les trois pages `player`, `display` et `gm` s'ouvrent dans le navigateur et affichent chacune un texte d'attente distinct.
- Étant donné les textes affichés, quand on inspecte les composants, alors aucun texte n'est écrit en dur : ils proviennent de `client/src/shared/i18n/fr.ts`.
- Étant donné `client/src/shared/theme.css`, quand une page s'affiche, alors ses couleurs et sa typographie proviennent des variables CSS du thème.
- Étant donné la page `player`, quand on l'ouvre en émulation mobile, alors le viewport n'est pas zoomable et les zones interactives portent `touch-action: manipulation`.
- Étant donné la configuration TypeScript, quand on l'inspecte, alors `strict: true` est actif.
- Étant donné le code du client, quand on lance `npm run check`, alors le typecheck (`svelte-check` et `tsc`), ESLint et la vérification Prettier passent. Un `any` ou un `@ts-ignore` sans commentaire fait échouer la commande.
- Étant donné un test Vitest d'exemple dans `client/src/shared/`, quand on lance `npm run test`, alors il passe.
- Étant donné un test Playwright d'exemple qui ouvre la page `player` en émulation Safari iOS et Chrome Android, quand on lance `npm run e2e`, alors il passe.
- Étant donné les pages construites, quand on les ouvre avec l'onglet réseau des outils de développement, alors aucune requête ne sort vers un domaine externe (polices et ressources embarquées).

**Comportement en cas d'erreur**
Sans objet pour les joueurs, le public et le GM à ce stade. Une erreur de type, de lint ou de format fait échouer `npm run check` avec le fichier et la ligne.

**Notes techniques**
- Arborescence : `client/src/player/`, `client/src/display/`, `client/src/gm/`, `client/src/shared/`, `client/src/modes/` (vide pour l'instant).
- Svelte 5 en runes uniquement : configurer le compilateur en mode runes.
- Dépendances npm à signaler, toutes en `devDependencies` et sans appel réseau à l'exécution : `vite`, `svelte`, `@sveltejs/vite-plugin-svelte`, `typescript`, `svelte-check`, `eslint`, `typescript-eslint`, `eslint-plugin-svelte`, `prettier`, `prettier-plugin-svelte`, `vitest`, `@playwright/test`.
- Les navigateurs Playwright se téléchargent à l'installation, pas à l'exécution du jeu.
- Une police auto-hébergée dans `client/src/assets/fonts/` peut être ajoutée ici ou reportée à E20 ; dans tous les cas, aucune police externe.
- Choix du front : [ADR 0002](../../docs/adr/0002-front-svelte-5.md).

**Hors périmètre**
- Build vers `wwwroot` et proxy vers le serveur .NET (US-E01-03).
- Types générés depuis `Contracts` (US-E01-04).
- Connexion SignalR et store du snapshot (E03).
- Routage `/`, `/display`, `/gm` côté serveur (E02).
