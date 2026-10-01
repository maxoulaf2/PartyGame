### US-E03-01 — Moteur pur : état immuable, transitions et effets

**Statut :** Prête

**Résultat attendu**
`PartyGame.Engine` expose une fonction pure `Handle(state, input, context)` qui retourne une `Transition` (nouvel état et effets). L'état de la partie est immuable et ne dépend ni de l'horloge, ni du hasard, ni d'aucune entrée/sortie. C'est le socle sur lequel le lobby (E04) puis les modes de jeu (E07) sont construits.

**Critères d'acceptation**
- Étant donné `PartyGame.Engine`, quand on l'inspecte, alors il contient `GameState`, `GameInput`, `GameContext`, `Transition` et `Effect`, et un point d'entrée `GameEngine.Handle(GameState, GameInput, GameContext)`.
- Étant donné l'état initial d'une partie, quand on le crée, alors il porte un `GameId`, une phase `Lobby` et une liste de joueurs vide.
- Étant donné une entrée valide, quand `Handle` la traite, alors il retourne un nouvel état (obtenu par `with`) et les effets éventuels, sans modifier l'état reçu.
- Étant donné une entrée rejetée par les règles (mauvaise phase, joueur inconnu…), quand `Handle` la traite, alors il retourne la même instance d'état (`ReferenceEquals`) et aucun effet.
- Étant donné un `GameContext` fourni par l'appelant, quand le moteur a besoin de l'heure ou du hasard, alors il utilise `context.Now` et `context.Random`, initialisé avec une graine contrôlée. Deux appels avec le même état, la même entrée et le même contexte produisent le même résultat.
- Étant donné les tests d'architecture existants, quand ils s'exécutent, alors ils vérifient toujours que `Engine` ne dépend que de `Contracts`, et un nouveau test vérifie qu'il n'appelle ni `DateTime.Now`, ni `DateTime.UtcNow`, ni `new Random()`.
- Étant donné les tests du moteur, quand ils s'exécutent, alors ils suivent le format Given/When/Then et les noms `Method_Scenario_ExpectedResult`.

**Comportement en cas d'erreur**
Sans objet pour les utilisateurs. Une exception levée par le moteur est un bug : elle est traitée par `GameLoop` (US-E03-02).

**Notes techniques**
- L'état est fait de `record` avec `ImmutableArray` et `ImmutableDictionary`. Il doit rester sérialisable tel quel, en vue de la persistance (E11).
- `GameInput` est une hiérarchie de `record` : les intentions des clients (`Intent`), enrichies par le hub de l'émetteur et de l'heure de réception, et les événements internes (`TimerElapsed`…). Les intentions concrètes du lobby sont ajoutées par E04.
- Les effets de départ : `ScheduleTimer` et `CancelTimer` (utilisés par US-E03-03). `PlayAudio` et `ReportIncident` viendront avec leurs épopées.
- `GameContext` contient au minimum `Now` (`DateTimeOffset`) et `Random`. Le `Random` à graine est créé par `GameLoop`, pas par le moteur.
- Commentaires XML sur les types publics du moteur.
- Le découpage en modes (`IGameMode`) est hors de cette US : le moteur ne connaît pour l'instant que les phases `Lobby` et `Started` (US-E04-05).

**Hors périmètre**
- Exécution des effets et file d'entrées (US-E03-02 et US-E03-03).
- Projections vers les clients (US-E03-05).
- Interface `IGameMode` et enchaînement des manches (E07).
