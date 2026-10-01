# E01 — Dépôt et outillage

**Phase :** 0. Socle technique
**Objectif :** un dépôt outillé, où `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `npm run check` et `npm run test` passent dès le premier jour, et où le serveur sert le front.

## User stories

| US | Titre | Statut | Dépend de |
|---|---|---|---|
| [US-E01-01](US-E01-01-solution-dotnet.md) | Solution .NET et projets de test | Terminée | — |
| [US-E01-02](US-E01-02-client-multi-entrees.md) | Client Vite + Svelte 5 multi-entrées | Prête | — |
| [US-E01-03](US-E01-03-integration-front-serveur.md) | Build du front vers `wwwroot` et proxy de développement | À faire | US-E01-01, US-E01-02 |
| [US-E01-04](US-E01-04-generation-types-ts.md) | Génération des types TypeScript depuis `PartyGame.Contracts` | À faire | US-E01-01, US-E01-02 |
| [US-E01-05](US-E01-05-logs-serilog.md) | Logs Serilog vers la console et un fichier tournant | Terminée | US-E01-01 |
| [US-E01-06](US-E01-06-adr-architecture.md) | ADR 0001 : architecture générale | Terminée | — |

## Décisions

Toutes les décisions qui bloquaient l'épopée sont prises.

1. **Front :** TypeScript et Svelte 5 ([ADR 0002](../../docs/adr/0002-front-svelte-5.md)).
2. **Génération des types TypeScript :** générateur maison par réflexion ([ADR 0003](../../docs/adr/0003-generation-types-typescript.md)).
3. **Gestion centralisée des versions NuGet :** décidée pendant US-E01-01, avec des versions centralisées dans `Directory.Packages.props`.

## Ordre de réalisation suggéré

US-E01-02, puis US-E01-03 et US-E01-04.
