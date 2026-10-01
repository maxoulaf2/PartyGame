### US-E01-01 — Solution .NET et projets de test

**Statut :** Terminée

**Résultat attendu**
Une solution .NET 10 contenant les quatre projets de `src/` et leurs projets de test, dont les références respectent le sens des dépendances, avec des réglages de compilation et de style communs.

**Critères d'acceptation**
- Étant donné un clone frais du dépôt avec le SDK .NET 10, quand on lance `dotnet build` à la racine, alors la solution compile sans avertissement.
- Étant donné la solution, quand on l'ouvre, alors elle contient `src/PartyGame.Server`, `src/PartyGame.Engine`, `src/PartyGame.Content`, `src/PartyGame.Contracts`, et un projet de test par projet de `src/` dans `tests/` (`PartyGame.Server.Tests`, `PartyGame.Engine.Tests`…).
- Étant donné les références entre projets, quand on les inspecte, alors `Contracts` ne dépend de rien, `Engine` et `Content` ne dépendent que de `Contracts`, et `Server` dépend des trois.
- Étant donné `PartyGame.Engine`, quand un test d'architecture inspecte ses dépendances, alors il échoue si l'assembly référence `Microsoft.AspNetCore.*` ou `Microsoft.AspNetCore.SignalR*`.
- Étant donné `Directory.Build.props` à la racine, quand on compile, alors `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors` et `AnalysisLevel` = `latest-recommended` s'appliquent à tous les projets.
- Étant donné le `.editorconfig` à la racine, quand on lance `dotnet format --verify-no-changes`, alors la commande passe sur la solution.
- Étant donné les projets de test, quand on lance `dotnet test`, alors xUnit s'exécute et au moins un test par projet passe (test d'architecture ou test de fumée).
- Étant donné `PartyGame.Server`, quand on lance `dotnet run --project src/PartyGame.Server`, alors le serveur démarre et répond sur un endpoint minimal (par exemple `/health`).
- Étant donné le dépôt, quand on compile ou teste, alors aucun fichier de `bin/`, `obj/` ou de résultats de test n'apparaît dans `git status` (`.gitignore` à jour).

**Comportement en cas d'erreur**
Sans objet pour les joueurs, le public et le GM. Une violation du sens des dépendances ou une règle de style non respectée fait échouer `dotnet test` ou `dotnet format --verify-no-changes`, avec un message qui nomme le projet et la règle.

**Notes techniques**
- Épingler la version du SDK dans `global.json`.
- Format de solution `.slnx`, par défaut avec .NET 10.
- Dépendances NuGet à signaler : xUnit et son runner, `Microsoft.NET.Test.Sdk`, `Microsoft.Extensions.TimeProvider.Testing` (`FakeTimeProvider`). Aucune ne fait d'appel réseau à l'exécution.
- À décider pendant l'US : gestion centralisée des versions avec `Directory.Packages.props` (recommandé pour garder des versions cohérentes entre les projets de test), ou versions déclarées dans chaque projet.
- Mettre à jour la section « Commandes » de CLAUDE.md si une commande diffère.

**Hors périmètre**
- Écoute sur `0.0.0.0`, routage des interfaces et QR code (E02).
- Hub SignalR et `GameLoop` (E03).
- Logs Serilog (US-E01-05).
- Intégration continue.
