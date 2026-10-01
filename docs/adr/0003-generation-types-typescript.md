# ADR 0003 — Génération des types TypeScript par un outil maison

**Statut :** Accepté
**Date :** 2026-10-01

## Contexte

Le protocole entre le serveur et les clients (snapshots, intentions, messages du hub) est défini en C# dans `PartyGame.Contracts`. Le front est écrit en TypeScript ([ADR 0002](0002-front-svelte-5.md)). Écrire les types à la main des deux côtés finirait par produire des écarts silencieux : un champ renommé côté serveur casserait un client sans qu'aucun compilateur ne le signale.

Les types générés doivent refléter exactement le JSON produit par System.Text.Json avec les conventions du projet :

- propriétés en camelCase ;
- énumérations sérialisées en chaînes ;
- champs nullables présents et valant `null` ;
- identifiants typés (`readonly record struct PlayerId(Guid Value)`) ;
- types polymorphes discriminés par un champ `type`, comme les activités d'un pack ;
- à partir de E03, l'interface `IGameClient` du hub.

Contraintes supplémentaires :

- `PartyGame.Contracts` ne dépend de rien ;
- aucun outil ne fait d'appel réseau ;
- le front doit compiler sans le SDK .NET, donc les types générés sont versionnés.

## Décision

Un générateur maison, `tools/PartyGame.TypeGen`, produit les types TypeScript par réflexion sur l'assembly `PartyGame.Contracts`.

### Fonctionnement

- Projet console .NET qui ne référence que `PartyGame.Contracts`, ajouté à la solution.
- Son cœur est une fonction pure : elle prend l'assembly et retourne les fichiers à écrire (chemin et contenu). Elle est testable sans toucher au disque.
- Lancement depuis `client/` avec `npm run generate:contracts`. La sortie va dans `client/src/shared/contracts/`.
- Chaque fichier généré commence par un en-tête indiquant qu'il est généré, qu'il ne doit pas être modifié à la main, et quelle commande le régénère.
- Le générateur produit directement un code au format final. Les fichiers générés sont exclus d'ESLint et de Prettier.

### Correspondance des types

| C# | TypeScript |
|---|---|
| `string`, `Guid`, `DateTimeOffset` | `string` |
| Types numériques | `number` |
| `bool` | `boolean` |
| `T?` (référence ou valeur) | `T \| null`, propriété toujours présente |
| `ImmutableArray<T>`, `IReadOnlyList<T>` et autres collections | `readonly T[]` |
| Dictionnaire à clé chaîne ou identifiant typé | `Readonly<Record<string, T>>` |
| `enum` | union des noms des membres, tels qu'écrits en C# (`"Player" \| "Display" \| "GameMaster"`) |
| `record` ou classe | `export interface`, propriétés en camelCase |
| Identifiant typé (`readonly record struct` à une seule propriété `Value`) | type marqué : `type PlayerId = string & { readonly __brand: "PlayerId" }` |
| Base `[JsonPolymorphic]` avec ses `[JsonDerivedType]` | une interface par sous-type, avec `type` typé par son littéral, et une union discriminée nommée comme la base |
| Interface non polymorphe (interface client du hub, comme `IGameClient`) | `export interface` du même nom, une méthode par message, au nom C# inchangé (la cible SignalR), retournant `void` |

- Sur le fil, un identifiant typé est une simple chaîne. Un convertisseur JSON générique, défini dans `PartyGame.Contracts`, en assure la sérialisation. Il ne s'appuie que sur System.Text.Json, inclus dans .NET.
- Le type marqué empêche, à la compilation, de passer un `RoundId` là où un `PlayerId` est attendu. Côté client, une valeur reçue du serveur garde son type. Un identifiant n'est jamais construit à la main.
- Un type ou une construction non prise en charge fait échouer le générateur. Le message d'erreur nomme le type et la propriété en cause. Le générateur ne produit jamais de `unknown` ou de `any` silencieux.
- L'interface client du hub (`IGameClient`, ajoutée avec US-E03-04) suit les mêmes principes : ses méthodes retournent `Task`, sans surcharge, et leurs paramètres suivent la correspondance ci-dessus. Une interface n'est acceptée qu'à la racine, jamais comme type de propriété (sauf base polymorphe).

### Détection des écarts

- **Dans `dotnet test`** : un test régénère les types en mémoire et les compare aux fichiers versionnés. Il échoue en cas d'écart, en indiquant la commande à lancer. Cette vérification fait foi, car le SDK .NET est toujours présent pour les tests .NET.
- **Dans `npm run check`** : la même vérification est lancée en mode `--verify` si `dotnet` est disponible. Sinon, elle est ignorée avec un avertissement, pour que le front reste vérifiable sans le SDK.

### Alternatives écartées

- **Schéma JSON avec `JsonSchemaExporter`, puis `json-schema-to-typescript` :** pipeline en deux étapes avec une dépendance npm. Les identifiants typés exigent un ajustement du schéma, et le TypeScript produit pour les unions est verbeux et mal nommé. Ce mécanisme reste une piste pour générer `pack.schema.json` en E06, ce qui fera l'objet d'une décision distincte.
- **Tapper :** exige un attribut dans `PartyGame.Contracts`, donc une dépendance NuGet contraire à la règle « `Contracts` ne dépend de rien ». La prise en charge des types polymorphes de System.Text.Json est incertaine, et le projet n'a qu'un seul mainteneur.
- **TypeGen :** pas d'unions discriminées. Les types nullables et les identifiants typés exigent des réglages manuels.
- **NJsonSchema / NSwag :** dépendance lourde, historiquement pensée pour Newtonsoft.Json. Produit un TypeScript de style classe, éloigné de nos conventions.
- **Reinforced.Typings :** peu maintenu, pas de polymorphisme.
- **Générateur de source Roslyn :** écrire des fichiers hors du dossier de compilation depuis un générateur de source est déconseillé, et la mise au point est plus complexe qu'un programme console, sans bénéfice ici.
- **OpenAPI :** les messages SignalR ne sont pas des endpoints HTTP.

## Conséquences

### Bénéfices

- Aucune dépendance NuGet ni npm.
- Le format de sortie est entièrement maîtrisé : unions discriminées, types marqués et, plus tard, interface du hub.
- Un écart entre C# et TypeScript est détecté par les tests, et non découvert en cours de partie.

### Coûts et contraintes

- Quelques centaines de lignes à écrire et à maintenir, avec leurs tests.
- Chaque nouvelle construction C# utilisée dans `Contracts` (générique, nouveau type de collection…) doit être prise en charge par le générateur avant d'être utilisée. L'échec explicite du générateur le rappelle.
- Les conventions JSON du serveur (camelCase, énumérations en chaînes, convertisseur des identifiants) et celles du générateur doivent rester alignées. Un test d'intégration compare donc le JSON réellement produit au type généré pour un DTO d'exemple.

### Suivi

- Tableau « Décisions » de CLAUDE.md : nouvelle ligne « Génération des types TypeScript ».
- US-E01-04 : critère sur `npm run check` ajusté à la détection décrite ci-dessus.
- Lors de la réalisation de US-E01-04 : ajout de `tools/` à l'arborescence de CLAUDE.md, et de `npm run generate:contracts` à sa section « Commandes ».
