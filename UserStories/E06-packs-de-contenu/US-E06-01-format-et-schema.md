### US-E06-01 — Format du descripteur et schéma généré

**Statut :** Prête

**En tant qu'**auteur de pack
**je veux** écrire le `pack.json` d'un pack avec l'autocomplétion et la validation de VS Code
**afin de** repérer mes erreurs pendant la saisie, sans lancer le serveur

**Critères d'acceptation**
- Étant donné un dossier `packs/<pack>/` contenant un `pack.json` qui référence le schéma par `$schema`, quand l'auteur l'ouvre dans VS Code, alors les propriétés sont proposées à la saisie, avec leur description en français au survol.
- Étant donné un `pack.json` sans `$schema`, quand il est ouvert dans VS Code, alors le schéma s'applique quand même, grâce à l'association déclarée dans `.vscode/settings.json` pour `packs/*/pack.json`.
- Étant donné l'enveloppe d'un pack, quand l'auteur la rédige, alors elle comporte : `formatVersion` (1), `title` (de 1 à 60 caractères), `description` (facultative, 200 caractères au plus) et `rounds`, une liste d'au moins une activité. Chaque activité porte un `type`, qui désigne son mode de jeu, et un `title`.
- Étant donné une propriété inconnue (faute de frappe comme `titel`), quand elle est saisie, alors VS Code la signale, et sa lecture par le serveur échoue de même.
- Étant donné un `type` d'activité inconnu, quand il est saisi, alors VS Code le signale et propose les valeurs admises.
- Étant donné une modification des types du descripteur, quand `npm run generate:contracts` est lancé, alors `schemas/pack.schema.json` est régénéré en même temps que les types TypeScript.
- Étant donné un schéma versionné qui ne correspond plus aux types C#, quand `dotnet test` s'exécute, alors un test échoue en indiquant la commande à lancer. `npm run check` fait la même vérification si `dotnet` est disponible.
- Étant donné la génération des types TypeScript, quand elle s'exécute, alors aucun type de `PartyGame.Contracts.Packs` n'apparaît dans `client/src/shared/contracts/`.
- Étant donné les tests de `PartyGame.Contracts`, quand ils s'exécutent, alors ils couvrent la lecture de l'enveloppe, le refus d'une propriété inconnue, le refus d'un `type` inconnu et le refus d'une `formatVersion` non prise en charge. La lecture d'un pack complet et valide est couverte par US-E08-01, qui déclare le premier type d'activité.

**Comportement en cas d'erreur**
Auteur : VS Code souligne l'erreur et en donne la raison. La lecture et la validation par le serveur, avec la localisation précise des erreurs, sont l'objet de US-E06-02. Joueurs, public et GM : rien, aucun pack n'est encore chargé dans cette US.

**Notes techniques**
- Décisions 1 à 3 du README et [ADR 0004](../../docs/adr/0004-format-et-modele-des-packs.md).
- Types dans `PartyGame.Contracts.Packs` : `PackDescriptor` pour l'enveloppe et une base polymorphe pour les activités (`[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]`), qui reçoit un `[JsonDerivedType]` par mode. Le premier type d'activité, `quiz`, est déclaré par US-E08-01, à réaliser juste après : d'ici là, la liste des types admis est vide et aucun pack ne peut être valide.
- Options de lecture définies une seule fois à côté des types (`PackJsonOptions`) : camelCase, énumérations en chaînes, `JsonUnmappedMemberHandling.Disallow`, sans commentaires ni virgules finales.
- Contraintes simples déclarées par les attributs de `System.ComponentModel.DataAnnotations` (`[Range]`, `[StringLength]`, `[Length]`), descriptions par `[Description]`. Le générateur les traduit en JSON Schema avec `TransformSchemaNode`. Ces attributs font partie de la BCL : aucune dépendance.
- Le générateur gagne une sortie hors de `client/` : `schemas/pack.schema.json`. Le fichier est formaté de façon stable (indentation, ordre des propriétés) pour que ses diffs restent lisibles.
- Le premier pack d'exemple, sous `packs/`, arrive avec US-E08-01. L'association de VS Code se vérifie d'ici là sur un `pack.json` de travail, non versionné.
- CLAUDE.md, section « Commandes » : mentionner que `npm run generate:contracts` produit aussi le schéma.

**Hors périmètre**
- Le descripteur d'une manche de quiz (US-E08-01).
- Le chargement et la validation par le serveur, médias compris (US-E06-02).
- Les packs zip et la validation en ligne de commande (E17).
