# E01 — Dépôt et outillage

**Phase :** 0. Socle technique
**Objectif :** un dépôt outillé, où `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `npm run check` et `npm run test` passent dès le premier jour, et où le serveur sert le front.

## User stories

| US | Titre | Statut | Dépend de |
|---|---|---|---|
| [US-E01-01](US-E01-01-solution-dotnet.md) | Solution .NET et projets de test | Terminée | — |
| [US-E01-02](US-E01-02-client-multi-entrees.md) | Client Vite + Svelte 5 multi-entrées | À faire | Décision Svelte / Blazor |
| [US-E01-03](US-E01-03-integration-front-serveur.md) | Build du front vers `wwwroot` et proxy de développement | À faire | US-E01-01, US-E01-02 |
| [US-E01-04](US-E01-04-generation-types-ts.md) | Génération des types TypeScript depuis `PartyGame.Contracts` | À faire | US-E01-01, US-E01-02, décision sur l'outil |
| [US-E01-05](US-E01-05-logs-serilog.md) | Logs Serilog vers la console et un fichier tournant | Terminée | US-E01-01 |
| [US-E01-06](US-E01-06-adr-architecture.md) | ADR 0001 : architecture générale | Terminée | — |

## Points à trancher avant de démarrer

Ces décisions bloquent US-E01-02 et US-E01-04. Chacune fait l'objet d'un ADR et met à jour le tableau « Décisions » de CLAUDE.md.

1. **Front : Svelte 5 ou Blazor WebAssembly.** Bloque US-E01-02, et par conséquent US-E01-03 et US-E01-04.
2. **Outil de génération des types TypeScript.** Bloque US-E01-04.
3. **Gestion centralisée des versions NuGet** (`Directory.Packages.props`). Décidé pendant US-E01-01 : versions centralisées dans `Directory.Packages.props`.

## Ordre de réalisation suggéré

US-E01-06 et US-E01-01 en parallèle, puis US-E01-05. Une fois la décision sur le front prise : US-E01-02, US-E01-03, puis US-E01-04.
