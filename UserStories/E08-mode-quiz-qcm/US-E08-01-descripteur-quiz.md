### US-E08-01 — Descripteur d'une manche de quiz

**Statut :** Terminée

**En tant qu'**auteur de pack
**je veux** décrire une manche de quiz QCM dans `pack.json`, guidé par le schéma et par une documentation complète
**afin d'**écrire mes questions sans lire le code, et d'apprendre au chargement tout ce qui ne va pas

**Critères d'acceptation**
- Étant donné une activité de `type` `quiz`, quand l'auteur la rédige, alors elle comporte :
  - `title` : le titre de la manche ;
  - `answerSeconds` : facultatif, la durée de réponse par défaut, de 5 à 120 s (20 s si absente) ;
  - `points` : facultatif, les points d'une bonne réponse, de 0 à 10 000 (1 000 si absent) ;
  - `speedBonus` : facultatif, le bonus de rapidité maximal, de 0 à 10 000 (0 si absent, c'est-à-dire sans bonus) ;
  - `shuffleChoices` : facultatif, le mélange des propositions (non si absent) ;
  - `questions` : de 1 à 50 questions.
- Étant donné une question, quand l'auteur la rédige, alors elle comporte `text` (de 1 à 200 caractères), `image` (facultative, chemin d'un média du pack), `answerSeconds` (facultatif, surcharge la durée de la manche) et `choices`, de 2 à 4 propositions. Chaque proposition a un `text` (de 1 à 80 caractères) et, pour la bonne, `"correct": true`.
- Étant donné une question sans bonne réponse, ou avec plusieurs, quand le pack est chargé, alors le mode signale le problème avec le chemin de la question.
- Étant donné deux propositions identiques dans une même question, sans tenir compte de la casse, des accents ni des espaces, quand le pack est chargé, alors le mode signale le doublon avec le chemin de la seconde.
- Étant donné le schéma régénéré, quand l'auteur saisit une activité `quiz` dans VS Code, alors ses propriétés sont proposées et documentées en français, et les bornes ci-dessus sont vérifiées à la saisie.
- Étant donné `docs/modes/quiz.md`, quand un auteur le lit, alors il y trouve les règles (déroulé piloté par le GM, temps de réponse, réponse définitive, barème, révélation, joueurs arrivés en cours de partie), les phases, et le format du descripteur avec un exemple complet.
- Étant donné le pack d'exemple `packs/quiz-exemple/`, quand le serveur le charge, alors il est valide. Il contient 10 questions réparties en deux manches de 5, au moins une image, une question à la durée surchargée, une manche avec bonus de rapidité et une manche aux propositions mélangées. Il sert à la démonstration du critère de sortie.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent la lecture d'un pack de quiz complet et chaque problème de cohérence du mode.

**Comportement en cas d'erreur**
Auteur et GM : un descripteur incohérent rend le pack invalide, avec un problème localisé, présenté au GM avant le lancement (US-E06-03). Joueurs et public : rien, un pack invalide ne peut pas être joué.

**Notes techniques**
- Types dans `PartyGame.Contracts.Packs` (`QuizRoundDescriptor`…), déclarés sur la base des activités par `[JsonDerivedType(..., "quiz")]` ([ADR 0004](../../docs/adr/0004-format-et-modele-des-packs.md)).
- Les bornes sont des contraintes simples déclarées par attributs. Les règles qui portent sur plusieurs valeurs (une seule bonne réponse, doublons) sont vérifiées par la méthode de validation du mode (`IGameMode`, US-E07-01). Codes par exemple : `QuizCorrectChoiceMissing`, `QuizCorrectChoiceDuplicated`, `QuizChoiceDuplicated`.
- La normalisation des propositions pour la détection des doublons est la même que celle des pseudos (US-E04-02), si elle s'y prête.
- L'image est un `MediaPath` : son existence, son extension et sa casse sont vérifiées par US-E06-02.
- Les images du pack d'exemple sont créées pour l'occasion, sans droits de tiers, et restent légères.
- Les textes du pack d'exemple sont en français : c'est du contenu destiné aux joueurs.
- Réalisation : `QuizRoundDescriptor` (`answerSeconds`, `points`, `speedBonus`, `shuffleChoices`, `questions`), `QuizQuestion` et `QuizChoice` dans `PartyGame.Contracts.Packs`. Les valeurs par défaut sont des initialiseurs C# : une propriété facultative absente prend sa valeur par défaut, et `null` est refusé, sauf pour `image` et `answerSeconds` d'une question, où il équivaut à l'absence. Les bornes et valeurs par défaut sont des constantes de `QuizRoundDescriptor`, reprises par les attributs.
- Réalisation : `MediaPath`, `readonly record struct` lu comme une simple chaîne par `TypedIdJsonConverterFactory`, est introduit ici (prévu par US-E06-02). `JsonSchemaExporter` ne voit pas au travers d'un convertisseur et produisait un schéma vide : `PackSchemaGenerator` traduit désormais tout identifiant typé en `"type": "string"` (ou `["string", "null"]` s'il est facultatif). `title` porte `[JsonPropertyOrder(-1)]` pour figurer avant les propriétés du quiz dans le schéma.
- Réalisation : décision 8 du README. `PackProblem` (code, fichier, chemin JSON, paramètres nommés) et l'énumération `PackProblemCode`, prévus par US-E06-02, sont introduits ici avec les trois codes du quiz, et générés en TypeScript. `IGameMode` gagne `Validate(descriptor, path)`, abstraite dans `GameMode<TDescriptor, TState>` : le chemin de l'activité (`$.rounds[1]`) est fourni par l'appelant, et chaque problème est localisé dessous, dans `pack.json` (`PackDescriptor.FileName`).
- Réalisation : `QuizMode` (`PartyGame.Engine.Modes.Quiz`) est enregistré dans `AddGameModes()`. Sa vérification est complète ; son déroulé est provisoire jusqu'à US-E08-02 : la manche (`QuizRound`, vide) se termine dès son démarrage, et `Handle` rejette toute entrée. `RoundsTests` remplace donc le mode quiz par `TestQuizMode` (`RemoveAll<IGameMode>`).
- Réalisation : la clé de comparaison des pseudos est extraite dans `TextComparison.Key` (espaces rognés et réduits, accents retirés, majuscules), commune aux pseudos et aux propositions. `QuizChoiceDuplicated` porte le paramètre `choice`, le texte de la seconde proposition.
- Réalisation : pack d'exemple `packs/quiz-exemple/` (deux manches de 5 questions, deux images PNG dessinées pour l'occasion, de 4 et 5 Ko). Tant que le serveur ne charge pas les packs (US-E06-02), `SampleQuizPackTests` (tests du moteur) le vérifie de la même façon : lecture stricte, contraintes de chaque objet, cohérence du mode, existence et casse des images, et contenu attendu. `RepositoryRoot` passe dans `tests/Shared` pour cela.
- Réalisation : tests `QuizRoundDescriptorTests` (Contracts : pack complet, valeurs par défaut, propriétés manquantes, mal typées ou inconnues, chaque borne), `QuizModeTests` (Engine : chaque problème, chemins, doublons à la casse, aux accents et aux espaces près, problèmes multiples, démarrage provisoire) et `PackSchemaGeneratorTests` (identifiant typé).

**Hors périmètre**
- Les propositions illustrées, les réponses multiples et le vrai ou faux.
- L'audio dans une question (E14, E15).
- Le guide de rédaction des packs et le pack couvrant tous les modes (E17).
