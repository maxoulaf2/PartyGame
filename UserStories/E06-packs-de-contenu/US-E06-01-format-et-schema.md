### US-E06-01 — Format du descripteur et schéma généré

**Statut :** Terminée

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
- Réalisation : System.Text.Json refuse une base `[JsonPolymorphic]` sans aucun type dérivé (`InvalidOperationException` dès que les options la décrivent, schéma compris). Le type `quiz` est donc déclaré dès cette US, par un `QuizRoundDescriptor` vide (seulement `title`, hérité de `RoundDescriptor`) ; US-E08-01 lui ajoute ses propriétés. Aucun pack n'est chargé d'ici là (US-E06-02 dépend de US-E08-01) : ce type provisoirement trop permissif est sans effet.
- Réalisation : `PackDescriptor` (`$schema` facultatif et ignoré, indispensable puisque toute propriété inconnue est refusée ; `formatVersion` ; `title` ; `description` ; `rounds`) et `RoundDescriptor` (`title`, de 1 à 60 caractères comme celui du pack) sont des `record` à propriétés `required` : une propriété obligatoire absente ou `null` fait échouer la lecture. `PackDescriptor.CurrentFormatVersion` vaut 1 ; une autre version est refusée par `[Range(1, 1)]`, traduit en `"const": 1` dans le schéma.
- Réalisation : `PackJsonOptions` ajoute `AllowOutOfOrderMetadataProperties` : par défaut, System.Text.Json exige que `type` soit la première propriété d'une activité, ce qu'un auteur ne devine pas. À savoir pour US-E06-02 : une activité sans `type` lève une `NotSupportedException`, et non une `JsonException` ; la lecture s'arrête à la première erreur ; et `Validator` ne vérifie les attributs que d'un objet, sans descendre dans les objets qu'il contient.
- Réalisation : `PackSchemaGenerator` (`tools/PartyGame.TypeGen`) s'appuie sur `JsonSchemaExporter` avec `PackJsonOptions`, qui produit déjà `additionalProperties: false` et la liste des propriétés obligatoires. Il ajoute les descriptions (celle de la propriété, sinon celle de son type ; celle d'un type d'activité est aussi portée par la valeur de son `type`, pour le survol de `"quiz"`), et traduit `[StringLength]`, `[Length]`, `[MinLength]`, `[MaxLength]` et `[Range]`. Tout autre attribut de validation fait échouer la génération, pour que l'éditeur n'accepte jamais en silence ce que le serveur refusera. Le fichier déclare le dialecte 2020-12, est indenté de 2 espaces, en LF, avec les accents lisibles.
- Réalisation : `PartyGame.TypeGen` accepte `--schema <fichier>`, que `npm run generate:contracts` et `npm run check` (`scripts/checkContracts.js`) passent avec `../schemas/pack.schema.json`. L'espace de noms `PartyGame.Contracts.Packs` est exclu de la génération TypeScript, comme celui de la sérialisation.
- Réalisation : tests `PackDescriptorTests` (Contracts : enveloppe, `type` placé n'importe où, propriété inconnue, `type` inconnu ou absent, propriété manquante ou mal typée, commentaires et virgules finales, contraintes dont `formatVersion`), `PackSchemaGeneratorTests` (sur des types de test : chaque attribut traduit, descriptions, base polymorphe, attributs non traduisibles, format stable), `GeneratedPackSchemaTests` (schéma versionné à jour, avec la commande à lancer) et `OutputFileTests`. `GeneratedContractsTests` vérifie qu'aucun type des packs n'apparaît dans les types TypeScript.
- Vérification dans VS Code, sur un `pack.json` de travail sans `$schema` sous `packs/` : `formatVersion` à 2, `title` vide, `titel` et un `type` inconnu sont signalés (« Value must be 1. », « String is shorter than the minimum length of 1. », « Property titel is not allowed. », « Value must be "quiz". »).

**Hors périmètre**
- Le descripteur d'une manche de quiz (US-E08-01).
- Le chargement et la validation par le serveur, médias compris (US-E06-02).
- Les packs zip et la validation en ligne de commande (E17).
