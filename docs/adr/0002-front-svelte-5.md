# ADR 0002 — Front en TypeScript et Svelte 5

**Statut :** Accepté
**Date :** 2026-10-01

## Contexte

Le front comprend trois interfaces : joueur, écran TV et game master. Elles sont servies par le serveur ASP.NET Core. Deux familles de solutions étaient envisagées : une application TypeScript compilée par Vite avec Svelte 5, ou Blazor WebAssembly, qui permettrait d'écrire le front en C#.

Les contraintes qui pèsent sur ce choix :

- **Des téléphones hétérogènes sur un Wi-Fi saturé.** Les joueurs ouvrent la page en scannant un QR code, souvent tous en même temps, au début de la soirée. Le volume à télécharger et le temps de démarrage sur un Android d'entrée de gamme comptent directement dans l'expérience.
- **Safari iOS et Chrome Android récents** sont les deux cibles mobiles obligatoires.
- **Des rechargements fréquents.** Après une mise en veille, le navigateur peut décharger la page. Elle doit redevenir interactive en quelques instants.
- **Le buzzer exige une capture précise** sur `pointerdown`, horodatée avec `performance.now()`. Ce code doit s'exécuter au plus près du navigateur, sans couche intermédiaire.
- **Des vues simples.** Les composants ne contiennent aucune logique de jeu : ils affichent le dernier snapshot reçu. Le serveur porte toute la complexité métier.
- **Le contrat est défini en C#** dans `PartyGame.Contracts`.

## Décision

Le front est écrit en TypeScript avec Svelte 5, en runes uniquement, et compilé par Vite sous la forme d'une application multi-entrées (`player`, `display`, `gm`).

Les types TypeScript du contrat sont générés depuis `PartyGame.Contracts` (voir [ADR 0003](0003-generation-types-typescript.md)), pour compenser l'absence de types partagés entre serveur et client.

### Alternatives écartées

- **Blazor WebAssembly :** il partage les DTO C# sans génération, mais le runtime .NET à télécharger pèse plusieurs mégaoctets. Le démarrage est lent sur un téléphone modeste et sur un Wi-Fi saturé, précisément au moment où tous les joueurs se connectent. La capture du buzzer et la synchronisation d'horloge passeraient par de l'interop JavaScript, ce qui ajoute une couche là où la précision compte le plus.
- **Un autre framework JavaScript (React, Vue…) :** il est viable, mais plus lourd à l'exécution pour des vues aussi simples. Svelte compile les composants en JavaScript direct, produit de petits bundles, et ses runes offrent une réactivité explicite adaptée à un store de snapshot.

## Conséquences

### Bénéfices

- Des bundles légers et un démarrage rapide sur mobile, même après un rechargement.
- Un accès direct aux API du navigateur (`pointerdown`, `performance.now()`, audio de l'écran TV), sans interop.
- Un outillage standard et entièrement local : Vite, svelte-check, ESLint, Prettier, Vitest, Playwright.

### Coûts et contraintes

- Deux langages dans le dépôt, et donc deux chaînes d'outillage à maintenir.
- Les types du contrat doivent être générés et maintenus synchronisés avec le C# ([ADR 0003](0003-generation-types-typescript.md)).
- La validation des intentions reste exclusivement côté serveur : aucun code métier n'est partagé avec le client, ce qui est de toute façon voulu par l'architecture ([ADR 0001](0001-architecture-generale.md)).

### Suivi

- Tableau « Décisions » de CLAUDE.md : ligne du front passée à « Retenu ».
- US-E01-02 débloquée.
