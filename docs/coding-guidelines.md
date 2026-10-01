# Conventions de code

Ce document complète CLAUDE.md : CLAUDE.md décrit ce qu'on construit, ce document décrit comment on l'écrit.

## Langues

| Élément | Langue |
|---|---|
| Code : identifiants, commentaires, messages de log, noms de tests | Anglais |
| Messages de commit, noms de branches | Anglais |
| Documentation : `docs/`, README, ADR | Français |
| Textes affichés aux joueurs, à l'écran TV et au GM | Français |

Les textes d'interface ne sont jamais écrits en dur dans les composants. Ils sont centralisés dans `client/src/shared/i18n/fr.ts`. Le serveur n'envoie jamais de texte destiné à un humain : il envoie des codes (`PackMediaMissing`, `RoundHandlerFailed`…) accompagnés de paramètres, et le client les traduit.

## Architecture serveur

### Une boucle unique par partie, sans verrous

Dans une API REST, chaque requête est indépendante. Ici, toutes les actions modifient le même état, et SignalR appelle les méthodes du hub en parallèle depuis plusieurs connexions. Pour éliminer toute concurrence :

- Chaque partie possède une file `Channel<GameInput>` et une boucle unique, `GameLoop`, exécutée dans un `BackgroundService`. Elle traite les entrées une par une.
- Les méthodes du hub ne touchent jamais à l'état. Elles valident la forme du message, l'enrichissent (joueur, heure de réception) et le déposent dans la file.
- Les timers (fin de manche, fenêtre d'arbitrage du buzzer) ne modifient pas l'état eux-mêmes : à échéance, ils déposent une entrée dans la file.
- Aucun `lock` ni collection concurrente dans le moteur : la boucle est le seul écrivain.

`GameInput` regroupe les intentions des clients (`Intent`) et les événements internes (`TimerElapsed`, `MediaFailed`…).

### Cœur fonctionnel, coquille impérative

Le moteur (`PartyGame.Engine`) est pur : aucune entrée/sortie, aucune horloge, aucun hasard propre.

```csharp
public Transition Handle(GameState state, GameInput input, GameContext context);

public sealed record Transition(GameState State, ImmutableArray<Effect> Effects);
```

- L'état est immuable : des `record` avec `ImmutableArray` et `ImmutableDictionary`, mis à jour avec `with`. Cela rend le retour arrière gratuit en cas d'erreur, la persistance triviale et les tests lisibles.
- Le temps (`context.Now`) et le hasard (`context.Random`, initialisé avec une graine contrôlée) sont fournis par le contexte. Le mélange des propositions d'un quiz est donc reproductible en test.
- Les effets décrivent ce qui doit se passer hors du moteur (`ScheduleTimer`, `PlayAudio`, `ReportIncident`). C'est `GameLoop`, dans `PartyGame.Server`, qui les exécute.

### Les rejets métier ne sont pas des exceptions

Une intention invalide (réponse hors délai, second buzz, mauvaise phase) est un cas normal. `Handle` retourne alors la même instance d'état sans effet, accompagnée d'un motif (`Transition.Rejection`), et `GameLoop` ne diffuse rien. Le motif sert au log `Debug` et à la réponse faite à l'émetteur quand l'intention en attend une (inscription refusée, par exemple). Les exceptions sont réservées aux bugs.

### Idempotence des intentions

Chaque intention porte un `ClientSeq`, un entier croissant propre à chaque joueur et conservé avec son jeton dans le `localStorage`. Le serveur ignore toute intention dont le `ClientSeq` est inférieur ou égal au dernier traité pour ce joueur. Un client peut donc renvoyer sans risque ses intentions non acquittées après une reconnexion.

### Projections et diffusion

- Les DTO de snapshot vivent dans `PartyGame.Contracts`. L'état du moteur n'est jamais sérialisé directement vers un client.
- Chaque mode fournit une projection explicite par rôle. Chaque snapshot porte un numéro de `Version`.
- Après chaque transition qui change l'état, `GameLoop` envoie la projection `Display` au groupe `display`, la projection `GameMaster` au groupe `gm`, et à chaque joueur sa propre projection `Player`.

### Persistance pour reprendre après un crash

Après chaque transition, l'état est écrit dans `data/current-game.json`, de façon asynchrone et atomique (écriture dans un fichier temporaire puis renommage). Au démarrage, si ce fichier existe, le GM se voit proposer de reprendre la partie. Les jetons des joueurs restant valides, les téléphones se reconnectent d'eux-mêmes.

## Gestion des erreurs

Principe directeur : une erreur reste invisible pour les joueurs et l'écran TV. Le GM est le seul informé, et seulement quand il peut agir.

| Catégorie | Exemple | Traitement | Visible par |
|---|---|---|---|
| Rejet métier | Buzz hors fenêtre, double réponse | État inchangé, log `Debug` | Personne |
| Incident réseau | Veille du téléphone, Wi-Fi instable | Reconnexion automatique, renvoi des intentions non acquittées | Indicateur discret après 3 s |
| Contenu invalide | MP3 manquant, champ obligatoire absent | Détecté au chargement du pack, partie non lançable | GM, avant la partie |
| Incident média en partie | Fichier audio illisible | L'écran TV le signale, le GM peut passer l'étape | GM |
| Bug | Exception dans un mode | Retour à l'état précédent, log `Error`, incident GM | GM |
| Crash du serveur | Processus arrêté | Redémarrage et reprise depuis l'état persisté | Brève reconnexion |

### Côté serveur

- **Échouer vite avant la partie, ne jamais échouer pendant.** Au démarrage et au chargement d'un pack, toute anomalie bloque avec un message précis. Une fois la partie lancée, rien ne doit l'interrompre.
- **Aucune exception ne sort de `GameLoop`.** Un `BackgroundService` qui laisse échapper une exception arrête l'application. Le traitement d'une entrée, l'exécution des effets, la diffusion et la persistance sont chacun protégés :

```csharp
Transition transition;
try
{
    transition = _engine.Handle(_state, input, CreateContext());
}
catch (Exception ex)
{
    _logger.LogError(ex, "Input {InputType} failed in round {RoundId}", input.GetType().Name, _state.CurrentRoundId);
    _incidents.Report(IncidentCode.RoundHandlerFailed, _state.CurrentRoundId);
    continue; // the previous state is kept: immutability makes rollback free
}
```

- Si une même manche échoue de façon répétée, le GM se voit proposer de la passer. Un bug dans un mode n'affecte jamais les autres manches.
- Les méthodes du hub ne lèvent jamais d'exception vers un client. Une entrée malformée est ignorée et journalisée en `Warning`. `HubException` n'est jamais utilisée pour transmettre un message. `EnableDetailedErrors` n'est activé qu'en développement.

### Côté client

- **Aucun message technique à l'écran.** Chaque vue de mode est enveloppée dans un `<svelte:boundary>` dont l'affichage de repli est l'écran d'attente neutre. L'erreur est remontée au serveur.
- **Handlers globaux.** `window.onerror` et `unhandledrejection` remontent les erreurs (y compris celles des gestionnaires d'événements, que les boundaries ne captent pas) via `ReportClientError`. Ces envois sont dédupliqués, limités en fréquence et mis en attente pendant une déconnexion.
- **Connexion.** Rien n'est affiché pendant les 3 premières secondes d'une coupure. Ensuite, un indicateur discret (« Reconnexion… ») apparaît. Les éléments interactifs restent désactivés jusqu'à la réception d'un snapshot frais.
- **File d'envoi.** Les intentions non acquittées sont conservées et renvoyées après reconnexion. Le serveur rejette celles qui ne sont plus pertinentes.
- **Interface optimiste.** Le choix d'un joueur s'affiche immédiatement comme « en attente », puis il est confirmé par le snapshot suivant. En cas de désaccord, le snapshot l'emporte, sans message.
- **Versions.** Un client ignore tout snapshot dont la version est inférieure ou égale à celle qu'il affiche. Le serveur transmet son identifiant de build à la connexion : si celui du client diffère (JavaScript en cache après une mise à jour), le client se recharge de lui-même.
- **Écran TV.** Il n'affiche jamais d'écran vide. Si un média échoue, il le signale au serveur et conserve l'affichage courant.

### Logs

- `ILogger` avec des messages structurés (gabarits `{PlayerId}`, jamais d'interpolation), et Serilog vers la console et un fichier tournant dans `logs/`.
- Niveaux :
  - `Debug` pour les rejets métier ;
  - `Information` pour le cycle de vie (partie, manche, arrivée d'un joueur) ;
  - `Warning` pour les entrées malformées et les erreurs remontées par les clients ;
  - `Error` pour les bugs.
- Pas de log `Information` dans les chemins fréquents, comme la synchronisation d'horloge.

## Conventions C#

- Réglages communs dans `Directory.Build.props` : `Nullable` activé, `TreatWarningsAsErrors`, `ImplicitUsings`, `AnalysisLevel` à `latest-recommended`.
- Un `.editorconfig` à la racine. `dotnet format --verify-no-changes` doit passer.
- Namespaces à portée de fichier, un type public par fichier, et le nom du fichier égal au nom du type.
- Classes `sealed` par défaut. Des `record` pour l'état, les DTO, les entrées et les effets.
- Nommage .NET standard : PascalCase pour les types et membres publics, `_camelCase` pour les champs privés, préfixe `I` pour les interfaces, suffixe `Async` pour les méthodes asynchrones.
- Identifiants typés pour éviter les confusions : `readonly record struct PlayerId(Guid Value)`, `RoundId`…
- Asynchrone : jamais de `async void`, de `.Result` ni de `.Wait()`. Le `CancellationToken` est propagé partout.
- Jamais de `DateTime.Now`, `DateTime.UtcNow` ou `new Random()` dans le moteur : passer par le contexte.
- Les modes de jeu sont enregistrés explicitement dans une méthode d'extension `AddGameModes()`, pas par scan réflexif.
- Hub fortement typé : `Hub<IGameClient>`, avec l'interface `IGameClient` définie dans `PartyGame.Contracts`.
- JSON sur le fil : propriétés en camelCase, énumérations sérialisées en chaînes (`JsonStringEnumConverter`), propriétés nullables toujours présentes. Ces conventions sont définies une seule fois dans `ContractJsonOptions` (`PartyGame.Contracts`), que tout sérialiseur parlant aux clients applique.
- Identifiants typés : `[JsonConverter(typeof(TypedIdJsonConverterFactory))]` sur le `readonly record struct`, pour qu'ils circulent comme de simples chaînes.

## Conventions TypeScript et Svelte

- `strict: true`. Pas de `any` (préférer `unknown` puis un affinage de type). Pas de `@ts-ignore` sans commentaire justificatif.
- ESLint et Prettier configurés dans le dépôt. `npm run check` doit passer.
- Svelte 5 en runes uniquement (`$state`, `$derived`, `$props`, `$effect`). L'état partagé vit dans des modules `.svelte.ts`.
- Les composants sont des vues : ils dérivent tout du snapshot et ne contiennent aucune logique de jeu. Un compte à rebours affiché est calculé à partir de l'échéance serveur contenue dans le snapshot et de l'écart d'horloge. Il ne décide jamais rien.
- Toute communication avec le serveur passe par `shared/connection` (`sendIntent`, store du snapshot). Aucun composant n'importe `@microsoft/signalr`.
- Les types générés dans `client/src/shared/contracts/` ne sont jamais modifiés à la main.
- Nommage :
  - composants en PascalCase (`AnswerButton.svelte`) ;
  - fichiers TypeScript en camelCase (`clockSync.ts`) ;
  - variables et fonctions en camelCase.
- Styles :
  - CSS scopé dans les composants ;
  - thème en variables CSS dans `shared/theme.css` ;
  - aucun framework CSS ;
  - polices auto-hébergées dans `client/src/assets/fonts/`.
- Interface joueur :
  - conçue d'abord pour le mobile ;
  - zones tactiles d'au moins 48 px ;
  - `touch-action: manipulation` sur les zones interactives ;
  - propositions distinguées par la couleur et par une forme ou une lettre, jamais par la couleur seule.
- Écran TV :
  - conçu pour une TV 1080p vue de loin, avec de grands caractères et un fort contraste ;
  - aucun élément essentiel collé aux bords, car certaines TV rognent l'image.

## Tests

- Le moteur se teste en Given/When/Then sur des fonctions pures, sans réseau ni horloge réelle. Les noms de tests sont en anglais, au format `Method_Scenario_ExpectedResult` (par exemple `Handle_BuzzAfterArbitrationWindow_IsIgnored`).
- Chaque mode de jeu a des tests pour :
  - chacune de ses transitions ;
  - chacun de ses cas de rejet ;
  - chaque couple phase/rôle, avec un test de non-fuite des informations secrètes.
- Le hub se teste en intégration avec `WebApplicationFactory` et un vrai client `Microsoft.AspNetCore.SignalR.Client`.
- La résilience est testée explicitement :
  - une exception injectée dans un mode laisse la boucle vivante et l'état inchangé ;
  - une reconnexion en pleine manche restitue le bon snapshot ;
  - un redémarrage reprend la partie depuis l'état persisté.
- Côté front, Vitest couvre la logique partagée (synchronisation d'horloge, file d'envoi, ordre des snapshots). Playwright couvre chaque mode de bout en bout avec trois joueurs, l'écran TV et le GM.
- La correction d'un bug commence par un test qui le reproduit.

## Git

- Commits au format Conventional Commits, en anglais : `type(scope): subject`. Le sujet est à l'impératif, en minuscules, sans point final, en 72 caractères maximum.
- Types : `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `perf`, `build`.
- Portées (scopes) : `engine`, `server`, `content`, `contracts`, `client`, `mode-<nom>`. Exemple : `feat(mode-quiz): add answer reveal phase`.
- Le corps du commit explique le pourquoi quand il n'est pas évident.
- Un commit correspond à un changement cohérent qui compile et passe les tests.
- Branches : `feat/short-description`, `fix/short-description`.

## Documentation

- Toute décision structurante fait l'objet d'un ADR en français dans `docs/adr/NNNN-titre-court.md`, en trois parties : Contexte, Décision, Conséquences. Le tableau « Décisions » de CLAUDE.md est mis à jour en conséquence.
- Chaque mode de jeu est documenté dans `docs/modes/<mode>.md` : règles, phases, et format du descripteur avec un exemple complet.
- Les commentaires de code, en anglais, expliquent le pourquoi et non le quoi. Commentaires XML sur les types publics de `PartyGame.Contracts` et `PartyGame.Engine`.

## Avant de considérer une tâche terminée

- `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`, `npm run check` et `npm run test` passent.
- Toute intention nouvelle ou modifiée est validée côté serveur, idempotente, et son cas de rejet est testé.
- Toute projection nouvelle ou modifiée a son test de non-fuite.
- Aucun message technique n'est visible par un utilisateur, et tout nouveau texte d'interface est dans `fr.ts`.
- Aucune requête réseau externe n'a été introduite.
- La documentation et `schemas/pack.schema.json` sont à jour si le comportement ou le format des packs a changé.
