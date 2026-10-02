### US-E06-03 — Choix du pack par le GM et erreurs de pack

**Statut :** Terminée

**En tant que** game master
**je veux** choisir dans le lobby le pack à jouer, et voir clairement pourquoi un pack est refusé
**afin de** lancer la soirée avec un contenu sûr, ou de corriger le pack avant le lancement

**Critères d'acceptation**
- Étant donné la console GM dans le lobby, quand elle s'affiche, alors elle liste les packs chargés avec leur titre, leur dossier, leurs manches (titre et mode) et leur état, valide ou invalide.
- Étant donné un pack valide, quand le GM le choisit, alors il devient le pack sélectionné, visible sur toutes les consoles GM ouvertes, et la TV affiche son titre dans le lobby.
- Étant donné un seul pack valide, quand la console s'affiche pour la première fois, alors ce pack est déjà sélectionné.
- Étant donné un pack invalide, quand le GM l'ouvre, alors il voit tous ses problèmes, chacun avec le fichier, le chemin dans le descripteur et un message en français qui reprend les paramètres (par exemple « Média introuvable : images/tour-eiffel.jpg »). Le pack ne peut pas être sélectionné.
- Étant donné aucun pack sélectionné, quand le GM regarde le bouton « Lancer la partie », alors il est désactivé, avec l'indication « Choisissez un pack ».
- Étant donné le GM qui corrige un pack sur le disque, quand il appuie sur « Actualiser les packs », alors le serveur recharge et revalide tous les packs. Si le pack sélectionné est devenu invalide ou a disparu, la sélection est annulée.
- Étant donné la partie lancée, quand les snapshots sont diffusés, alors le pack sélectionné est figé pour toute la partie : la liste des packs et « Actualiser les packs » disparaissent de la console, et une modification des fichiers sur le disque n'a plus d'effet.
- Étant donné aucun pack trouvé, quand la console s'affiche, alors elle indique « Aucun pack trouvé » avec le dossier parcouru.
- Étant donné les projections `Display` et `Player`, quand les tests de non-fuite s'exécutent, alors elles ne contiennent que le titre du pack sélectionné, jamais son contenu ni la liste des packs.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent la sélection acceptée, les refus (pack inconnu, invalide, partie lancée), le rechargement, l'annulation de la sélection et le refus du lancement sans pack sélectionné. Un test Vitest vérifie que chaque code de problème a un texte dans `fr.ts`. Un test E2E choisit un pack, puis affiche les problèmes d'un pack invalide.

**Comportement en cas d'erreur**
GM : connexion perdue, la console est verrouillée comme d'habitude (US-E05-02). La sélection est rejouable sans risque : choisir deux fois le même pack ne change rien. Un rechargement qui échoue sur un pack le marque invalide (US-E06-02), sans affecter les autres. Joueurs et public : rien, la TV garde le lobby.

**Notes techniques**
- Intentions GM `SelectPack(packId)` et `ReloadPacks`, marquées `[GameMasterOnly]` et refusées hors du lobby.
- Le moteur reste pur : le chargement des fichiers se fait hors de la boucle, puis le serveur dépose une entrée qui porte le résultat (`PacksLoaded`), au démarrage et après chaque rechargement. Le catalogue (packs et problèmes) entre ainsi dans l'état, comme les adresses candidates de US-E04-06, et se diffuse comme le reste.
- Au lancement, le descripteur du pack sélectionné est copié dans l'état de la partie : il est persisté avec elle (E11), et la partie ne dépend plus du disque.
- Les messages des problèmes sont dans `fr.ts`, un par code, avec les paramètres interpolés. Le chemin dans le descripteur est affiché tel quel : l'auteur le retrouve dans son fichier.
- E2E : un dossier de packs dédié aux tests, qui contient au moins un pack valide et un pack invalide, est passé au serveur par `Packs:Directory`.
- Réalisation : le catalogue entre dans l'état (`GameState.Catalog`, types `PackCatalog` et `CatalogPack` du moteur, sans chemin sur le disque ni exception), avec `SelectedPackId` et `Pack`, le descripteur copié au lancement. `GameState.Rounds` est désormais lu dans `Pack`. Le catalogue de démarrage est passé à `GameState.Create` plutôt que par une entrée `PacksLoaded` : chargé avant que Kestrel n'écoute, il est dans l'état initial, et aucune console ne peut voir un catalogue vide le temps que la boucle traite l'entrée. `PacksLoaded` sert aux actualisations.
- Réalisation : règles du moteur dans `PackChoice` (`SelectPack`, `PacksLoaded`) et `Launch`. Nouveaux motifs de rejet : `PackUnknown`, `PackInvalid`, `PackNotSelected` ; hors du lobby, `GameAlreadyStarted`. Choisir le pack déjà choisi est accepté sans changement d'état, donc sans diffusion. Choix d'office et annulation : décision 6 du README.
- Réalisation : hub. `SelectPack` (`SelectPackRequest` → `SelectPackResult`, refus `PackUnknown`, `PackInvalid`, `AlreadyStarted`, `SelectionFailed`, `MessageInvalid`) et `ReloadPacks` (sans message → `ReloadPacksResult`, refus `AlreadyStarted`, `ReloadFailed`), tous deux `[GameMasterOnly]`. `StartGameRefusal` gagne `PackNotSelected`. L'actualisation passe par `PackReloader` : lecture des fichiers hors de la boucle par `PackLibraryLoader` (le chargement et les logs de US-E06-02, réutilisés), puis dépôt de `PacksLoaded` ; un verrou n'autorise qu'une actualisation à la fois, pour qu'une lecture plus ancienne ne remplace jamais une plus récente. Logs `Information` : pack choisi, packs actualisés, et pack joué au lancement.
- Réalisation : projections. `GameMasterSnapshot` porte `PackCatalog` (dossier, puis pour chaque pack : identifiant, titre, nombre de manches, validité, manches avec leur titre et leur mode, problèmes), `null` hors du lobby, ainsi que `SelectedPackId` et `PackTitle`. `DisplaySnapshot` ne porte que `PackTitle`. `PlayerSnapshot` ne contient rien des packs : les téléphones n'en affichent rien. Le mode d'une manche est le `type` de l'activité (`GameModes.TypeOf`), traduit par le client (`fr.gm.packs.modes`).
- Réalisation : client. `PackControl.svelte` liste les packs sous forme de boutons radio (un pack invalide est désactivé), avec un `<details>` « Voir les problèmes » pour les packs invalides et le bouton « Actualiser les packs ». `describeProblem` (`gm/packProblemText.ts`) traduit chaque code, choisit la formulation d'une borne selon `min` et `max` (un minimum de 0 ne borne rien), et décrit le type attendu de `PackValueTypeInvalid` en mots. Le texte de chaque code est typé par l'union générée (`Record<PackProblemCode, …>`), et `fr.test.ts` vérifie en plus à l'exécution, depuis `PackProblemCode.ts`, que chaque code a son texte.
- Réalisation : tests. Moteur : `PackChoiceTests`, `LaunchTests`, `GameStateTests`, et dans `SnapshotsTests` le catalogue du GM et la non-fuite des projections `Display` et `Player` (ni autres packs, ni problèmes, ni dossier, ni manches avant leur lancement). Hub : `PackChoiceTests` (serveur de test sur un dossier de packs temporaire, `TestPacks`). Les tests du hub qui lancent une partie lisent désormais un pack (un seul, choisi d'office). Front : `packProblemText.test.ts`, `fr.test.ts` et `gameMasterSession.test.ts`. E2E : `packs.spec.ts` (choix d'un pack et titre sur la TV, problèmes d'un pack invalide, actualisation), le dossier `client/e2e/packs` (deux packs valides, pour qu'aucun ne soit choisi d'office, et un invalide), vérifié aussi par `RepositoryPacksTests`. `launch.spec.ts` choisit le pack avant de lancer, et `reconnection.spec.ts` vérifie le verrouillage sur « Actualiser les packs » plutôt que sur « Lancer la partie », qui dépend désormais du pack choisi par les autres tests.

**Hors périmètre**
- Le dépôt d'un pack depuis le navigateur.
- Le mode aperçu d'un pack (E17).
- Le choix d'un sous-ensemble de manches et le réordonnancement (E19).
