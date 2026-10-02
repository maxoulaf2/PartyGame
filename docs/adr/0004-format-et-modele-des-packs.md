# ADR 0004 — Format et modèle des packs de contenu

**Statut :** Accepté
**Date :** 2026-10-02

## Contexte

Un pack est un dossier qui contient un descripteur et ses médias. Le format du descripteur était provisoirement JSON, avec YAML comme alternative (tableau « Décisions » de CLAUDE.md). La phase 2 livre le premier mode de jeu, le quiz QCM, et donc le premier format de pack réellement utilisé : la décision ne peut plus attendre.

Trois questions se posent ensemble :

- **Le format du fichier.** Les auteurs de packs l'écrivent à la main. Ils ont besoin d'autocomplétion et d'erreurs signalées pendant la saisie, sans lancer le serveur.
- **L'emplacement des types C# du descripteur.** `PartyGame.Content` lit et valide les packs. Les modes de jeu, dans `PartyGame.Engine`, se servent de leur contenu pour dérouler une manche. Or `Content` et `Engine` ne dépendent que de `PartyGame.Contracts`, et l'un ne peut pas référencer l'autre.
- **La production du schéma `schemas/pack.schema.json`.** CLAUDE.md impose de le tenir à jour à chaque évolution du format. L'[ADR 0003](0003-generation-types-typescript.md) a renvoyé à une décision distincte la piste de `JsonSchemaExporter`.

Contraintes :

- aucune dépendance sans justification, et aucun appel réseau à l'exécution ;
- un pack est entièrement validé au chargement, et chaque erreur est localisée (fichier, chemin dans le descripteur, problème) ;
- ajouter un mode ne doit nécessiter aucune modification du moteur en dehors de son enregistrement.

## Décision

### Format

- Le descripteur est un fichier JSON, `pack.json`, à la racine du dossier du pack. Il est lu avec System.Text.Json.
- La lecture est stricte : JSON standard, sans commentaires ni virgules finales. Une propriété inconnue est une erreur (`JsonUnmappedMemberHandling.Disallow`), pour qu'une faute de frappe ne soit jamais ignorée en silence.
- Les conventions de nommage sont celles du fil : propriétés en camelCase, énumérations en chaînes. Elles sont définies une seule fois dans des options dédiées aux packs, à côté des types du descripteur.
- Le descripteur porte un numéro de version du format (`formatVersion`, 1 pour la phase 2), pour pouvoir le faire évoluer plus tard.
- Chaque activité porte un champ `type`, qui désigne son mode de jeu. Ce champ sert de discriminant à la désérialisation polymorphe.
- Un pack est un dossier. Les packs au format zip sont reportés en E17.

### Emplacement des types

- Les types du descripteur vivent dans `PartyGame.Contracts`, dans l'espace de noms `PartyGame.Contracts.Packs`. Le format de pack est un contrat, passé avec les auteurs plutôt qu'avec les clients.
- Cet espace de noms est exclu de la génération des types TypeScript : les clients ne reçoivent jamais un descripteur, seulement des projections.
- `PartyGame.Content` les lit et les valide. Les modes de `PartyGame.Engine` les consomment directement. Le sens des dépendances reste inchangé.
- La base polymorphe des activités déclare un `[JsonDerivedType]` par mode. Ajouter cette ligne fait partie de l'enregistrement d'un mode.
- Les problèmes détectés dans un pack sont décrits par des codes et des paramètres, dans `PartyGame.Contracts`, et traduits par le client. Ils sont transmis à l'interface GM et donc générés en TypeScript.

### Schéma

- `schemas/pack.schema.json` est généré depuis les types C# par `tools/PartyGame.TypeGen`, avec `JsonSchemaExporter`, inclus dans .NET. La même commande produit les types TypeScript et le schéma.
- Le schéma est enrichi lors de la génération :
  - `additionalProperties: false` sur chaque objet, comme la lecture stricte ;
  - les contraintes simples (bornes, longueurs, nombre d'éléments), déclarées une seule fois sur les types par les attributs standard de `System.ComponentModel.DataAnnotations`, et lues aussi par la validation de `PartyGame.Content` ;
  - une description en français de chaque type et de chaque propriété, portée par l'attribut `[Description]` de `System.ComponentModel`. Le schéma documente le format pour les auteurs : il suit la règle des documents en français, alors que les commentaires XML restent en anglais.
- Comme pour les types TypeScript, un test de `dotnet test` régénère le schéma en mémoire et échoue en cas d'écart avec le fichier versionné, en indiquant la commande à lancer. `npm run check` fait la même vérification si `dotnet` est disponible.
- Chaque `pack.json` référence le schéma par sa propriété `$schema`. Le fichier `.vscode/settings.json`, versionné, associe aussi le schéma aux fichiers `packs/*/pack.json`.

### Alternatives écartées

- **YAML :** plus agréable à écrire et commentable, mais il faut une dépendance NuGet (YamlDotNet) et une extension VS Code pour profiter du schéma. Les positions d'erreur et la lecture stricte demandent un travail supplémentaire.
- **Types du descripteur dans `PartyGame.Content`, référencé par `PartyGame.Engine` :** inverse le sens des dépendances documenté dans CLAUDE.md et lie le moteur pur au chargement des fichiers.
- **Un modèle dans `Content`, un autre dans `Engine`, et une conversion dans le serveur :** isolation maximale, mais chaque mode touche alors trois projets de plus, avec une conversion à maintenir.
- **Schéma écrit à la main :** plus riche (exemples, messages personnalisés), mais il divergerait du code. Le vérifier exigerait un validateur JSON Schema, donc une dépendance.
- **Validation des packs par le schéma au chargement :** dépendance supplémentaire, et des messages moins précis que ceux d'une validation écrite en C#. Le schéma sert à l'édition ; le serveur valide avec son propre code.

## Conséquences

### Bénéfices

- Aucune dépendance NuGet ni npm.
- Une seule source de vérité pour le format : les types C#, d'où découlent la lecture, les contraintes et le schéma.
- Les auteurs bénéficient de l'autocomplétion, de la documentation au survol et des erreurs à la saisie dans VS Code.
- Une faute de frappe dans un nom de propriété est détectée, dans l'éditeur comme au chargement.

### Coûts et contraintes

- `PartyGame.Contracts` contient des types qui ne circulent pas sur le fil. Le générateur TypeScript doit exclure leur espace de noms.
- Le générateur gagne une seconde sortie, avec ses tests.
- JSON reste verbeux à écrire à la main. Les outils de création de contenu (E17) atténueront ce coût.
- Les descriptions françaises du schéma sont des textes dans le code C#, exception assumée à la règle « le serveur n'envoie jamais de texte destiné à un humain » : elles ne sont jamais envoyées aux clients.

### Suivi

- Tableau « Décisions » de CLAUDE.md : la ligne « Descripteurs de packs en JSON » passe à « Retenu ».
- CLAUDE.md, section « Packs de contenu » : zip reporté en E17.
- Roadmap : zip ajouté à E17.
- US impactées : US-E06-01 (format, schéma), US-E06-02 (lecture stricte et validation), US-E08-01 (descripteur du quiz).
- Lors de la réalisation de US-E06-01 : section « Commandes » de CLAUDE.md, pour la génération du schéma.
