### US-E16-01 — Descripteur d'une manche de questions ouvertes

**Statut :** Terminée

**En tant qu'** auteur de pack
**je veux** décrire une manche de questions ouvertes, avec pour chaque question sa réponse attendue et ses variantes acceptées
**afin de** préparer des questions à réponse libre que le serveur saura pré-classer

**Critères d'acceptation**
- Étant donné un `pack.json` dont une activité a le `type` `openquestion`, quand le pack est chargé, alors la manche est lue avec son titre, sa durée de réponse (`answerSeconds`, 30 s si absente, de 5 à 120 s), ses points (`points`, 1 000 si absents), son bonus de rapidité (`speedBonus`, 0 si absent), la longueur maximale d'une réponse (`maxLength`, 40 caractères si absente, de 5 à 100) et ses questions (de 1 à 50).
- Étant donné une question, quand elle est lue, alors elle a un texte (1 à 200 caractères), une image facultative, une durée de réponse facultative, une réponse attendue (`answer`, 1 à `maxLength` caractères), des variantes acceptées facultatives (`acceptedAnswers`, 0 à 20, chacune de 1 à `maxLength` caractères) et un type de clavier (`inputMode` : `text` par défaut, ou `numeric`).
- Étant donné une question `numeric`, quand le pack est validé, alors sa réponse et ses variantes ne contiennent que des chiffres, sinon le pack est refusé avec un problème précis.
- Étant donné une réponse attendue ou une variante vide une fois normalisée (« ! », « Les »), ou deux variantes identiques une fois normalisées, quand le pack est validé, alors il est refusé avec le chemin de la question fautive.
- Étant donné une erreur dans le descripteur, quand le GM ouvre le pack dans la console, alors chaque problème s'affiche en français avec le fichier, le chemin dans le descripteur et les paramètres (US-E06-03).
- Étant donné le schéma `schemas/pack.schema.json`, quand il est régénéré, alors il décrit la manche et ses questions, avec leurs bornes, pour VS Code.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent un descripteur valide et chaque problème (bornes, réponse numérique invalide, réponse vide après normalisation, variantes en double, image introuvable).

**Comportement en cas d'erreur**
Contenu invalide : pack non sélectionnable, problèmes listés au GM avant la partie. Joueurs et public : rien.

**Notes techniques**
- `OpenQuestionRoundDescriptor` et `OpenQuestionDescriptor` dans `PartyGame.Contracts.Packs`, `OpenQuestionMode` enregistré dans `AddGameModes()` avec sa vérification (`IGameMode.Validate`) et un déroulé provisoire, comme le quiz en US-E08-01 (décision 8 du README de E08).
- La normalisation (décision 6 du README) vit dans le moteur (`PartyGame.Engine/Modes/OpenQuestion`), en fonction pure testée à part ; la validation du pack et le pré-classement (US-E16-03) l'utilisent tous deux.
- `docs/modes/openquestion.md` décrit le descripteur avec un exemple complet ; un pack d'exemple `packs/openquestion-exemple` l'accompagne.

**Hors périmètre**
- Plusieurs réponses attendues à donner ensemble (« citez trois planètes »).
- Réponses numériques jugées à la plus proche.
