# E17 — Outils de création de contenu

**Phase :** 5. Questions ouvertes et contenu
**Objectif :** qu'un auteur écrive un pack sans lire le code. Il suit un guide, part d'un pack d'exemple qui couvre tous les modes, vérifie son pack en ligne de commande, le fait défiler sur la TV avant la soirée, et le partage en un seul fichier zip.

## User stories

| US | Titre | Statut | Dépend de |
|---|---|---|---|
| [US-E17-01](US-E17-01-validation-en-ligne-de-commande.md) | Validation d'un pack en ligne de commande | Terminée | — |
| [US-E17-02](US-E17-02-packs-zip.md) | Packs au format zip | Terminée | — |
| [US-E17-03](US-E17-03-apercu-sur-la-tv.md) | Aperçu d'un pack sur la TV | Terminée | — |
| [US-E17-04](US-E17-04-guide-et-pack-complet.md) | Guide de rédaction et pack d'exemple de tous les modes | À faire | US-E16-04, US-E17-01, US-E17-02 |

## Décisions

Toutes les décisions qui bloquaient l'épopée sont prises (2026-10-05).

1. **Validation par une commande du serveur :** `PartyGame.Server validate <chemin>` vérifie un pack (dossier ou zip), ou tous les packs d'un dossier, avec le code de chargement du serveur, puis s'arrête sans écouter le réseau. Elle marche aussi avec l'exécutable publié pour le Raspberry Pi. Options écartées : un outil console dédié (`tools/PartyGame.PackTool`), et un script npm.
2. **Zip extraits au chargement :** au démarrage et à chaque actualisation, chaque `.zip` du dossier des packs est extrait dans un cache (`packs-cache` sous `Persistence:Directory`), puis chargé comme un dossier. Le service des médias et les requêtes partielles (Range) ne changent pas. Le cache d'un zip est réutilisé tant que sa taille et sa date de modification ne changent pas. Option écartée : des médias lus directement dans l'archive, dont les entrées compressées ne permettent pas d'accès direct à une position.
3. **Aperçu sur la TV piloté par la console GM :** dans le lobby, le GM choisit un pack valide et en fait défiler chaque question sur la TV, réponse comprise, extraits audio joués à la demande. Les téléphones restent sur le lobby. L'aperçu est refusé une fois la partie lancée. Options écartées : une page `/preview/` autonome, et un aperçu dans la console GM seule.
4. **Messages des problèmes en français dans la commande :** la console n'a pas de client pour traduire les codes. La commande `validate` affiche donc chaque problème en français, avec le fichier, le chemin dans le descripteur et le code, à partir d'une table .NET qui couvre tous les codes (vérifié par un test). Les messages de la console GM restent dans `fr.ts`. Choix de réalisation, ajustable sans nouvelle décision.

## Ordre de réalisation suggéré

1. US-E17-01, US-E17-02 et US-E17-03, en parallèle de E16.
2. US-E17-04 une fois le mode question ouverte terminé.

## Critère de sortie (phase 5)

Un pack mêlant QCM, blind test et questions ouvertes est écrit par une personne qui suit le guide sans lire le code, validé avec `validate`, partagé en zip, parcouru en aperçu sur la TV, puis joué de bout en bout sur de vrais appareils (au moins un iPhone sous Safari et un Android sous Chrome), avec l'écran TV et la console GM, serveur lancé par `scripts/start.ps1`.

`dotnet test`, `dotnet format --verify-no-changes`, `npm run check`, `npm run test` et `npm run e2e` passent.
