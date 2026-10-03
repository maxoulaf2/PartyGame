### US-E08-02 — Présentation de la question

**Statut :** Terminée

**En tant que** public
**je veux** voir la question et ses propositions avant l'ouverture des réponses
**afin que** chacun ait le temps de lire avant que le compte à rebours ne démarre

**Critères d'acceptation**
- Étant donné une manche de quiz qui démarre, quand le snapshot est diffusé, alors sa première question est en phase `Presentation`.
- Étant donné une question en présentation, quand la TV l'affiche, alors elle montre le titre de la manche, le numéro de la question sur le total (« Question 3/5 »), son texte, son image s'il y en a une, et ses propositions, chacune avec sa lettre, sa forme et sa couleur.
- Étant donné la TV en 1080p, quand une question de 200 caractères a quatre propositions de 80 caractères, alors tout est lisible à 3 m, sans défilement ni texte coupé, et rien d'essentiel ne se trouve à moins de 5 % d'un bord.
- Étant donné un téléphone de joueur, quand la question est en présentation, alors il affiche le numéro de la question et un bouton par proposition, avec sa lettre, sa forme et sa couleur, désactivés, avec l'indication de lire la question sur l'écran : ni la question ni le texte des propositions (décision 9 du README).
- Étant donné la console GM, quand la question est en présentation, alors elle affiche la question, les propositions avec la bonne réponse signalée par une icône et un libellé (jamais par la couleur seule), et les boutons « Ouvrir les réponses » et « Passer la question » (US-E08-05).
- Étant donné une manche avec `shuffleChoices`, quand une question est présentée, alors ses propositions sont mélangées, dans le même ordre sur tous les écrans. Le mélange est reproductible en test avec une graine fixée.
- Étant donné un joueur inscrit pendant la présentation, quand les réponses s'ouvriront, alors il participera à la question (décision 5 du README).
- Étant donné les projections `Display` et `Player` en phase `Presentation`, quand les tests de non-fuite s'exécutent (US-E07-03), alors elles sont identiques quelle que soit la bonne réponse.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent le démarrage de la manche, le mélange reproductible et les projections des trois rôles. Un test E2E vérifie la question sur la TV, sur un téléphone et sur la console GM, avec la bonne réponse visible seulement sur cette dernière.

**Comportement en cas d'erreur**
Public : une image qui ne se charge pas laisse la question affichée sans elle (US-E06-04). Joueurs : connexion perdue, le téléphone garde ses boutons affichés et verrouillés (US-E05-02). GM : idem.

**Notes techniques**
- Les propositions sont identifiées dans les projections et les intentions par leur position affichée (A à D), jamais par leur position dans le descripteur : sinon, l'ordre d'origine, souvent « bonne réponse en premier », se lirait à travers le mélange.
- Formes associées aux lettres, par exemple : A triangle, B losange, C cercle, D carré. Couleurs et formes sont des variables du thème (`shared/theme.css`).
- L'image passe par l'URL opaque de US-E06-04, présente dans la projection `Display` seulement.
- Intention GM `OpenAnswers(round, question)`, livrée avec US-E08-03.
- Réalisation : contrats dans `PartyGame.Contracts.Quiz`. `QuizDisplayView`, `QuizPlayerView` et `QuizGameMasterView` portent le numéro de la question, le nombre de questions de la manche, la phase (`QuizQuestionPhase`, réduite à `Presentation` : chaque phase arrive avec son US, pour que la suite de non-fuite exige ses scénarios) et les propositions dans l'ordre affiché ; la TV et la console ont en plus le texte de la question. `QuizPlayerView` ne porte que les lettres des propositions (décision 9 du README). Une proposition (`QuizChoiceView`) porte sa lettre (`QuizChoiceLetter`, `A` à `D`, sérialisée en chaîne) et son texte ; celle du GM (`QuizGameMasterChoice`) porte en plus `correct`. Seule la vue TV a `imageUrl`, l'URL opaque de US-E06-04. Les intentions `quiz` restent les types provisoires de E07 jusqu'à US-E08-03.
- Réalisation : moteur. `QuizRound` garde le descripteur de la manche, l'index de la question en cours, sa phase (`QuizPhase`) et `ChoiceOrder`, la position dans le descripteur de chaque proposition affichée (la première est A). `QuizMode.Start` présente la première question ; le mélange (`shuffleChoices`) est tiré par `context.Random.Shuffle` à la présentation de chaque question, donc reproductible avec une graine. Faute d'intention jouable, la manche reste sur la présentation de sa première question : `Handle` rejette les intentions provisoires (`RejectionReason.IntentUnsupported`, nouveau) et les timers (`UnexpectedTimer`).
- Réalisation : client. `ChoiceMarker.svelte` (forme et lettre) et `choiceTheme.ts` restent dans `modes/quiz/`, seul mode à s'en servir. Le thème définit `--choice-<lettre>-color` et `--choice-<lettre>-shape`, une forme étant un `clip-path` (A triangle, B losange, C cercle, D carré). TV : titre de la manche et « Question 3/5 » en tête, question en `h1` avec son image à gauche (au plus 35 % de la largeur et 32 % de la hauteur), propositions en grille 2 × 2 ; une image qui échoue est retirée et la question reste. Téléphone : un pavé de boutons colorés en grille 2 × 2, chacun avec sa forme et sa lettre en sombre sur la couleur de la proposition (nom accessible « Proposition A »), désactivés, avec « Lis la question sur l’écran : les réponses vont s’ouvrir. ». `ChoiceMarker` accepte pour cela une couleur de forme (`currentColor`). Console GM : la bonne réponse porte une coche et le libellé « Bonne réponse » ; « Ouvrir les réponses » et « Passer la question » sont affichés désactivés jusqu'à leurs US.
- Réalisation : tests. Moteur : `QuizModeTests` (démarrage, ordre du descripteur, mélange reproductible, rejets, projections des trois rôles, même ordre sur tous les écrans, joueur inscrit pendant la présentation, numérotation, aucun texte dans la projection d'un téléphone) et `QuizLeakTests`, première `LeakSuite` d'un mode (cinq scénarios ; questions à venir et leurs images cachées hors GM, chemins des images cachés à tous ; paires « bonne réponse » avec et sans mélange), avec `QuizGames` pour bâtir les parties. Hub : `StartGameTests` vérifie la question sur les trois rôles, la bonne réponse sur la console seule, et un joueur arrivé pendant la présentation ; `TestQuizMode` (`RoundsTests`) délègue désormais ses vues au vrai `QuizMode`. E2E : `display.spec.ts` (présentation, formes distinctes, question de 200 caractères avec image et quatre propositions de 80 caractères en 1080p, image en échec), `gm.spec.ts` (bonne réponse signalée, boutons désactivés) et `launch.spec.ts`, qui s'arrête désormais sur la présentation de la première question (TV, téléphone sans texte, retardataire et console). `address.spec.ts` passe dans un projet Playwright `address`, exécuté avant `launch` : la TV quitte le lobby et son QR code au lancement.

**Hors périmètre**
- L'ouverture des réponses et le compte à rebours (US-E08-03).
- L'écran d'introduction de la manche (E18).
- Les animations d'apparition (E20).
