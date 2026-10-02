# E06 — Packs de contenu (v1)

**Phase :** 2. Premier mode : quiz QCM
**Objectif :** un auteur écrit un pack dans VS Code avec l'autocomplétion et les erreurs signalées à la saisie. Le serveur charge et valide entièrement chaque pack au démarrage, le GM choisit dans le lobby celui à jouer, et voit avant le lancement pourquoi un pack est refusé. Une partie ne peut donc jamais échouer en cours de route à cause de son contenu.

## User stories

| US | Titre | Statut | Dépend de |
|---|---|---|---|
| [US-E06-01](US-E06-01-format-et-schema.md) | Format du descripteur et schéma généré | Terminée | — |
| [US-E06-02](US-E06-02-chargement-et-validation.md) | Chargement et validation des packs au démarrage | Terminée | US-E06-01, US-E08-01 |
| [US-E06-03](US-E06-03-choix-du-pack.md) | Choix du pack par le GM et erreurs de pack | Terminée | US-E06-02 |
| [US-E06-04](US-E06-04-service-des-medias.md) | Médias du pack servis sur le réseau local | Terminée | US-E06-03 |

## Décisions

Toutes les décisions qui bloquaient l'épopée sont prises.

1. **Format JSON :** le descripteur `pack.json` est lu strictement avec System.Text.Json : pas de commentaires, et toute propriété inconnue est une erreur ([ADR 0004](../../docs/adr/0004-format-et-modele-des-packs.md)). Option écartée : YAML, qui ajoute une dépendance et une extension VS Code.
2. **Types du descripteur dans `PartyGame.Contracts.Packs` :** lus par `Content`, consommés par les modes de `Engine`, exclus de la génération TypeScript ([ADR 0004](../../docs/adr/0004-format-et-modele-des-packs.md)). Options écartées : `Engine` qui référence `Content`, et deux modèles reliés par une conversion dans le serveur.
3. **Schéma généré :** `schemas/pack.schema.json` est produit depuis les types C# par `PartyGame.TypeGen` avec `JsonSchemaExporter`, et un test détecte tout écart ([ADR 0004](../../docs/adr/0004-format-et-modele-des-packs.md)). Option écartée : un schéma écrit à la main.
4. **Packs zip reportés :** en phase 2, un pack est un dossier. Le format zip arrive avec les outils de création de contenu (E17), quand le partage de packs devient utile. Il faudra alors extraire les médias pour servir les requêtes partielles.
5. **Codes de problème précis** (décidé pendant US-E06-02) : un code par nature de problème, pour que le client (US-E06-03) formule un message juste sans interpréter les paramètres. Les bornes ont trois codes, selon ce qu'elles limitent : une valeur (`PackValueOutOfRange`), un nombre de caractères (`PackTextLengthOutOfRange`) ou un nombre d'éléments (`PackItemCountOutOfRange`). Un type incorrect a son propre code (`PackValueTypeInvalid`, avec le type attendu), tout comme un chemin de média mal écrit (`PackMediaPathInvalid`). Option écartée : un seul `PackValueOutOfRange` pour toutes les bornes, que le client n'aurait pas su dire en caractères, en éléments ou en valeur.
6. **Choix automatique et annulation de la sélection** (décidé pendant US-E06-03) : sans sélection, un pack valide unique est choisi d'office, au démarrage comme après une actualisation. Une sélection annulée par une actualisation (pack devenu invalide ou disparu) n'est jamais remplacée par un autre pack, même seul valide : la partie ne doit pas jouer un pack que le GM n'a pas choisi sans qu'il s'en aperçoive. Option écartée : ne choisir d'office qu'au tout premier affichage de la console.

## Ordre de réalisation suggéré

1. US-E06-01, en parallèle de US-E07-01.
2. US-E08-01, qui définit le premier type d'activité, puis US-E06-02.
3. US-E06-03, puis US-E06-04.

## Critère de sortie (phase 2)

Porté par E09 : une partie de 10 questions jouée de bout en bout par trois joueurs, et les tests de non-fuite qui passent pour chaque phase et chaque rôle.
