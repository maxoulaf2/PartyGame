### US-E06-02 — Chargement et validation des packs au démarrage

**Statut :** Terminée

**En tant qu'**opérateur
**je veux** que le serveur charge et vérifie entièrement chaque pack dès son démarrage
**afin qu'**aucune partie n'échoue en cours de route à cause de son contenu

**Critères d'acceptation**
- Étant donné le dossier des packs, quand le serveur démarre, alors chaque sous-dossier contenant un `pack.json` est chargé et validé avant que le hub n'accepte de connexion. Un sous-dossier sans `pack.json` est ignoré.
- Étant donné le chargement terminé, quand l'opérateur lit la console, alors il voit la liste des packs, avec pour chacun son dossier, son titre, son nombre de manches et son état : valide, ou invalide avec le nombre de problèmes.
- Étant donné un `pack.json` mal formé (virgule manquante), quand il est chargé, alors le pack est invalide, avec un problème qui donne le fichier, la ligne et la colonne.
- Étant donné une erreur de structure (propriété obligatoire absente, type incorrect, propriété inconnue, `type` d'activité inconnu, valeur hors bornes), quand le pack est chargé, alors le problème indique le fichier et le chemin dans le descripteur (par exemple `$.rounds[1].questions[4].choices`).
- Étant donné un média référencé, quand le pack est chargé, alors chacun de ces cas rend le pack invalide, avec un problème distinct :
  - le fichier est absent ;
  - le chemin sort du dossier du pack (`../`, chemin absolu) ;
  - l'extension n'est pas prise en charge ;
  - la casse du chemin diffère de celle du fichier (`Image.PNG` pour `image.png`), car le Raspberry Pi y est sensible alors que Windows ne l'est pas.
- Étant donné un pack qui comporte plusieurs erreurs, quand il est chargé, alors toutes sont rapportées en une fois, sans s'arrêter à la première. Seule une erreur de syntaxe JSON empêche d'aller plus loin.
- Étant donné une incohérence propre à un mode (par exemple, une question sans bonne réponse), quand le pack est chargé, alors le mode la signale avec le chemin concerné (US-E08-01).
- Étant donné un pack valide, quand il est chargé, alors son descripteur est conservé en mémoire : aucune relecture du `pack.json` n'a lieu pendant une partie.
- Étant donné un dossier des packs absent ou vide, quand le serveur démarre, alors il démarre quand même, et la console le signale.
- Étant donné les tests de `PartyGame.Content`, quand ils s'exécutent, alors ils couvrent chaque problème ci-dessus avec un pack de test dédié, et un test vérifie que tous les packs du dossier `packs/` du dépôt sont valides.

**Comportement en cas d'erreur**
Opérateur : un pack invalide n'empêche jamais le démarrage. Il est signalé dans la console et ne pourra pas être choisi (US-E06-03). Une exception pendant le chargement d'un pack (bug) le marque invalide, avec un problème générique, un log `Error`, et les autres packs se chargent normalement. Joueurs et public : rien.

**Notes techniques**
- Dossier configurable par une option typée (`Packs:Directory`), par défaut `packs` relatif au dossier de l'application. `scripts/start.ps1` pointe vers le dossier `packs/` du dépôt.
- Identifiant d'un pack : le nom de son dossier.
- Problèmes décrits dans `PartyGame.Contracts` (`PackProblem` : code, fichier, chemin, paramètres), sans texte destiné à un humain. Ils seront transmis au GM et traduits par le client (US-E06-03). Codes par exemple : `PackJsonInvalid`, `PackPropertyMissing`, `PackPropertyUnknown`, `PackValueOutOfRange`, `PackRoundTypeUnknown`, `PackMediaMissing`, `PackMediaOutsidePack`, `PackMediaTypeUnsupported`, `PackMediaCaseMismatch`, `PackLoadFailed`. `PackProblem` et `PackProblemCode` existent depuis US-E08-01, avec les codes du quiz : cette US y ajoute les siens.
- Une erreur de syntaxe se lit dans `JsonException` (`Path`, `LineNumber`, `BytePositionInLine`). Les contraintes simples se vérifient à partir des mêmes attributs que le schéma (ADR 0004).
- Un type dédié aux chemins de médias (`MediaPath`, chaîne sur le fil) existe depuis US-E08-01 (image d'une question de quiz). Il permet à `Content` de trouver tous les médias d'un descripteur sans connaître les modes. Les chemins sont relatifs au dossier du pack, avec `/` pour séparateur. Extensions prises en charge en phase 2 : `.jpg`, `.jpeg`, `.png`, `.webp`.
- La cohérence propre à un mode est vérifiée par le mode lui-même (`IGameMode`, US-E07-01), qui retourne des `PackProblem`. `IGameMode.Validate(descriptor, path)` existe depuis US-E08-01 (décision 8 du README de E08), et `QuizMode` est enregistré : il suffit de lui passer le chemin de chaque activité (`$.rounds[1]`). `Content` ne référence pas le moteur : le serveur lui fournit les vérifications des modes enregistrés.
- Logs : `Information` pour chaque pack chargé, `Warning` pour chaque problème (contenu invalide, pas un bug), `Error` pour une exception.
- Réalisation : `PackLoader` (`PartyGame.Content`) charge un pack ou tout le dossier (`LoadAll`) et retourne une `PackLibrary` de `LoadedPack` : identifiant, dossier, titre et nombre de manches (lus même dans un pack invalide, pour la console), descripteur (seulement s'il est valide) et problèmes. Une exception pendant le chargement d'un pack donne `PackLoadFailed`, avec l'exception dans `LoadedPack.Failure` pour le log `Error` du serveur.
- Réalisation : pour tout rapporter en une fois, alors que System.Text.Json s'arrête à la première erreur, `DescriptorReader` parcourt le `JsonDocument` en suivant le contrat que System.Text.Json déduit des types du descripteur avec `PackJsonOptions` (`JsonTypeInfo` : propriétés, `IsRequired`, `IsSetNullable`, polymorphisme). La lecture et la vérification ne peuvent donc pas diverger. Chaque partie lisible est désérialisée seule pour vérifier les attributs de contrainte de ses propriétés, repérer ses `MediaPath` et confier ses activités à leur mode, même si d'autres parties du descripteur sont illisibles. Le descripteur complet n'est désérialisé que si aucun problème n'a été trouvé.
- Réalisation : codes ajoutés à `PackProblemCode` (décision 5 du README) : `PackJsonInvalid` (`line`, `column`, comptés en caractères et non en octets), `PackPropertyMissing` (chemin de l'objet, `property`), `PackPropertyUnknown` (chemin de la propriété, `property`), `PackValueTypeInvalid` (`expected`), `PackValueOutOfRange`, `PackTextLengthOutOfRange` et `PackItemCountOutOfRange` (`min`, `max`), `PackRoundTypeUnknown` (`type`), `PackMediaPathInvalid`, `PackMediaOutsidePack`, `PackMediaTypeUnsupported` (`extension`), `PackMediaMissing`, `PackMediaCaseMismatch` (`actual`), tous avec `media`, et `PackLoadFailed`.
- Réalisation : un chemin de média est invalide (`PackMediaPathInvalid`) s'il est vide, utilise `\`, contient un segment vide ou `.`, ou un caractère refusé par Windows dans un nom de fichier. Une extension non prise en charge n'empêche pas de vérifier l'existence du fichier. La casse est comparée segment par segment, sur tous les systèmes. Le `pack.json` d'un dossier doit lui aussi porter ce nom exact, faute de quoi le dossier est ignoré, sous Windows comme sur le Pi.
- Réalisation : `GameModes.Validate` (moteur) confie une activité à son mode, ou retourne `PackRoundTypeUnknown` si aucun mode enregistré ne la joue. Le serveur la passe à `PackLoader` comme `RoundValidator`. `AddGameModes()` devient idempotent (`TryAddEnumerable`), car le chargement des packs (`AddPacks()`) et la boucle de jeu l'appellent tous deux.
- Réalisation : `Program.cs` charge les packs (`app.LoadPacks()`) avant `app.Run()`, donc avant que Kestrel n'écoute. La bannière de la console liste les packs ; une option relative (`Packs:Directory`) est résolue depuis `AppContext.BaseDirectory`.
- Réalisation : tests `PackLibraryTests`, `DescriptorProblemsTests` et `MediaProblemsTests` (un dossier de packs temporaire par test, avec un pack dédié à chaque cas), `RepositoryPacksTests` (packs du dépôt vérifiés avec les vrais modes, d'où une référence du projet de test `PartyGame.Content.Tests` vers `PartyGame.Engine`), `GameModesTests`, `StartupBannerTests` et `PackStartupTests` (serveur réel : packs valides et invalides, dossier absent, logs structurés). `SampleQuizPackTests` (moteur), qui vérifiait le pack d'exemple en attendant cette US, est remplacé par `RepositoryPacksTests`.

**Hors périmètre**
- L'affichage des problèmes au GM, la sélection et le rechargement des packs (US-E06-03).
- Le service des médias (US-E06-04).
- Les vérifications propres à l'audio : durée, point de départ d'un extrait (E14).
- Les packs zip et la validation en ligne de commande (E17).
