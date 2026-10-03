### US-E07-03 — Aucune fuite d'information, quel que soit le mode

**Statut :** Terminée

**En tant que** joueur
**je veux** qu'aucun écran autre que celui du GM ne trahisse une bonne réponse ou le choix d'un autre joueur avant la révélation
**afin que** la partie reste loyale, même si quelqu'un ouvre l'écran TV ou les outils de développement de son téléphone

**Critères d'acceptation**
- Étant donné un mode, quand ses tests de non-fuite sont écrits, alors ils s'appuient sur un outil commun, dans `tests/Shared`, qui fournit les deux vérifications ci-dessous.
- Étant donné deux états qui ne diffèrent que par un secret (la bonne réponse, ou le choix d'un autre joueur), quand l'outil compare leurs projections `Display` et `Player` sérialisées en JSON avec `ContractJsonOptions`, alors elles doivent être identiques. Cette comparaison détecte aussi une fuite par un index, un ordre ou un compteur, qu'une recherche de texte ne verrait pas.
- Étant donné des valeurs secrètes repérables (jetons des joueurs, textes marqueurs), quand l'outil sérialise toutes les projections d'un état, alors il échoue si l'une d'elles apparaît là où elle est interdite.
- Étant donné un échec, quand il est signalé, alors le message nomme la phase, le rôle, le joueur le cas échéant, et le chemin JSON de la première différence ou de la valeur trouvée.
- Étant donné un mode, quand il déclare ses phases, alors l'outil échoue si l'une d'elles n'est couverte par aucun scénario, pour chaque rôle : une phase ajoutée plus tard ne peut pas échapper au test.
- Étant donné les tests de non-fuite existants du lobby (jetons, code GM, adresses candidates), quand l'outil est disponible, alors ils l'utilisent à leur tour.
- Étant donné la projection `GameMaster`, quand l'outil s'exécute, alors il ne la contraint pas sur les réponses : elle est la seule autorisée à les contenir avant la révélation. Elle ne contient jamais les jetons.

**Comportement en cas d'erreur**
Sans objet à l'exécution : cette US ne livre que des tests. Un test de non-fuite qui échoue bloque la tâche, comme tout test.

**Notes techniques**
- Un mode fournit des scénarios : une liste d'états qui couvre chacune de ses phases, et pour chaque secret, une paire d'états qui ne diffèrent que par lui. Le quiz est le premier à en fournir (US-E08-02 à US-E08-04).
- La comparaison se fait sur le JSON sérialisé, et non sur les objets : c'est ce que reçoit réellement un client.
- La couverture des phases s'appuie sur une énumération des phases déclarée par le mode.
- La convention « chaque couple phase/rôle a son test de non-fuite » de `docs/coding-guidelines.md` est satisfaite par cet outil.
- Réalisation : l'outil est dans `tests/Shared/Leaks/`, compilé dans chaque projet de test, et ne dépend que de `PartyGame.Contracts`. `LeakSuite<TState, TPhase>` reçoit la phase d'un état (`PhaseOf`), ses projections (`Project`, un `ProjectionSet` : écran TV, GM et chaque téléphone, sérialisés avec `ContractJsonOptions` ou les options du test), les scénarios, les secrets d'un état (`SecretsOf`) et les paires (`SecretPair`). Elle expose `AssertEveryPhaseIsCovered`, `AssertNoSecretIsShown` et `AssertPairsLookTheSame`. Les états des paires comptent aussi comme scénarios.
- Réalisation : un secret (`Secret`) est caché à une `Audience` : `Everyone` (jetons, chemins des médias), `AllButGameMaster` (bonnes réponses, adresses et packs), `OtherPlayersThan(joueur)` (pseudo et identifiant d'un joueur), `AllButGameMasterAnd(joueur)` (choix d'un joueur avant la révélation). La projection GM n'est contrainte que par les secrets cachés à `Everyone`.
- Réalisation : la recherche parcourt le JSON décodé (valeurs texte et noms de propriétés, qui portent des données dans un dictionnaire) et non le texte brut : le sérialiseur écrit `\u00C9chauffement`, si bien que l'ancienne recherche de « Échauffement » dans `PackChoiceTests` ne pouvait rien trouver. La comparaison des paires rapporte le chemin de la première différence (`$.choices[0].correct is true in one state, false in the other`), y compris une propriété ou un élément de tableau présent d'un seul côté. Chaque échec nomme le scénario, la phase, le rôle et le joueur.
- Réalisation : les tests de non-fuite du lobby (`SnapshotsTests`) sont regroupés dans `SnapshotsLeakTests`, une `LeakSuite` sur `GamePhase` avec quatre scénarios par phase (trois joueurs, autres packs, pack illustré, autre adresse choisie) et deux paires par phase (sans jetons pour tous les rôles, sans les secrets du GM pour l'écran TV et les téléphones). Les tests d'intégration du hub (jetons, code GM, adresses candidates, packs, chemins des médias) passent par `LeakAssert.NoSecretReceived`, qui nomme la version et la phase du snapshot fautif et échoue si aucun snapshot n'a été reçu.
- Réalisation : l'outil est lui-même testé sur un mode jouet (`Engine.Tests/Leaks/LeakSuiteTests.cs`) dont chaque test fait fuiter une projection : texte, nom de propriété, caractère non ASCII, index, ordre, compteur, téléphone d'un autre joueur, phase ou rôle non couvert.

**Hors périmètre**
- Les scénarios propres au quiz, écrits avec chacune de ses US.
- La vérification du trafic réseau réel dans les tests E2E.
