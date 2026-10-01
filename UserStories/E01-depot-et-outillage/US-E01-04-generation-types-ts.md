### US-E01-04 — Génération des types TypeScript depuis `PartyGame.Contracts`

**Statut :** À faire

**Résultat attendu**
Les DTO et messages de `PartyGame.Contracts` sont disponibles en TypeScript dans `client/src/shared/contracts/`, générés par une commande unique, et une désynchronisation entre C# et TypeScript est détectée automatiquement.

**Critères d'acceptation**
- Étant donné un DTO d'exemple dans `PartyGame.Contracts` (un `record` avec une énumération, un champ nullable et une collection), quand on lance la commande de génération, alors le type TypeScript correspondant apparaît dans `client/src/shared/contracts/`.
- Étant donné les conventions JSON du fil, quand on compare les types générés au JSON produit par le serveur, alors les propriétés sont en camelCase, les énumérations sont des unions de chaînes et les champs nullables sont typés `T | null`.
- Étant donné un identifiant typé (`readonly record struct PlayerId(Guid Value)`), quand il est sérialisé, alors il apparaît sur le fil comme une simple chaîne, et le type généré est un type marqué (`string & { readonly __brand: "PlayerId" }`).
- Étant donné un type polymorphe (`[JsonPolymorphic]` discriminé par `type`), quand il est généré, alors on obtient une interface par sous-type et une union discriminée sur `type`.
- Étant donné un type non pris en charge dans `Contracts`, quand on lance la génération, alors elle échoue avec un message qui nomme le type et la propriété.
- Étant donné un DTO modifié en C# sans régénération, quand on lance `dotnet test`, alors un test échoue en signalant que les types générés ne sont pas à jour et en indiquant la commande à lancer.
- Étant donné la même situation, quand on lance `npm run check` avec le SDK .NET disponible, alors la commande échoue de la même façon. Sans SDK, cette vérification est ignorée avec un avertissement.
- Étant donné les fichiers générés, quand on les ouvre, alors un en-tête indique qu'ils sont générés et ne doivent pas être modifiés à la main.
- Étant donné les fichiers générés, quand ESLint et Prettier s'exécutent, alors ils passent ou les fichiers sont explicitement exclus.

**Comportement en cas d'erreur**
Sans objet pour les joueurs, le public et le GM. Une génération en échec ou des types obsolètes font échouer `npm run check` avec un message qui indique la commande à lancer.

**Notes techniques**
- Outil retenu : générateur maison par réflexion, `tools/PartyGame.TypeGen`, lancé par `npm run generate:contracts` ([ADR 0003](../../docs/adr/0003-generation-types-typescript.md), qui fixe la correspondance des types).
- Le convertisseur JSON des identifiants typés est défini dans `PartyGame.Contracts`.
- Mettre à jour l'arborescence et la section « Commandes » de CLAUDE.md.
- Les types générés sont versionnés dans le dépôt, pour que le front compile sans SDK .NET.
- Toute dépendance NuGet ou npm ajoutée est signalée et justifiée.
- Dépend de US-E01-01 et US-E01-02.

**Hors périmètre**
- Les DTO réels du protocole (snapshots, intentions) : ils arrivent avec E03.
- Le typage de l'interface `IGameClient` du hub côté client (E03).
