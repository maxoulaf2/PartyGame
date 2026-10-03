### US-E11-01 — Enregistrement de la partie après chaque transition

**Statut :** Prête

**En tant que** game master
**je veux** que chaque changement de la partie soit enregistré sur le disque au fil de l'eau
**afin de** ne rien perdre si le serveur s'arrête brutalement

**Critères d'acceptation**
- Étant donné une transition qui change l'état, quand `GameLoop` l'a appliquée, alors l'état complet est écrit dans `current-game.json`, dans le dossier de données (`Persistence:Directory`, par défaut `data` à côté de l'exécutable).
- Étant donné l'écriture, quand elle a lieu, alors elle est atomique : écriture dans un fichier temporaire du même dossier, puis remplacement. Un arrêt brutal pendant l'écriture laisse l'ancien fichier intact, et un fichier temporaire abandonné est ignoré puis supprimé au démarrage suivant.
- Étant donné une rafale de transitions (dix joueurs qui répondent en même temps), quand elles s'enchaînent, alors la boucle n'attend jamais le disque : l'écriture est asynchrone, et seul le dernier état en attente est écrit. Un état plus ancien n'écrase jamais un état plus récent.
- Étant donné le fichier écrit, quand on l'ouvre, alors il contient la version du format, l'heure serveur de l'enregistrement (via `TimeProvider`) et l'état complet : joueurs, jetons, dernier `ClientSeq` de chaque joueur, scores, pack joué, identifiants des médias, manche en cours avec l'état propre à son mode. Il ne contient jamais le code GM.
- Étant donné un arrêt normal du serveur (Ctrl+C, arrêt du service), quand il s'arrête, alors le dernier état est écrit avant la sortie du processus.
- Étant donné une écriture qui échoue (disque plein, dossier en lecture seule), quand l'échec survient, alors la partie continue sans interruption, l'erreur est journalisée en `Error`, et un incident `PersistenceFailed` est signalé au GM (US-E10-01), une seule fois par série d'échecs. Il disparaît du compteur à la première écriture réussie.
- Étant donné un dossier de données inutilisable au démarrage (impossible à créer ou à écrire), quand le serveur démarre, alors il s'arrête avec un message précis, comme pour un port déjà utilisé : échouer vite avant la partie.
- Étant donné chaque état des scénarios de la `LeakSuite` de chaque mode enregistré, quand il est écrit puis relu, alors l'état relu est égal à l'original. Un nouveau mode est ainsi couvert sans test supplémentaire.

**Comportement en cas d'erreur**
Joueurs et public : rien. GM : un incident `PersistenceFailed` tant que l'enregistrement échoue, pour qu'il sache qu'un crash ne serait pas rattrapé.

**Notes techniques**
- Un listener `IGameStateListener` dédié, à côté de `SnapshotBroadcaster`, qui confie l'état à une tâche d'écriture unique (canal borné à un élément, dernier état gagnant). L'état étant immuable, aucune copie n'est nécessaire.
- Sérialisation avec System.Text.Json : `GameState` est déjà prévu pour (`[JsonIgnore]` sur les propriétés calculées). L'état d'une manche est polymorphe (`RoundState`) : ses types dérivés sont déclarés par les modes enregistrés dans `AddGameModes()`, sans scan réflexif, comme les descripteurs du pack. Le format du fichier n'est pas un contrat client : il n'utilise pas `ContractJsonOptions` s'il a besoin d'autres réglages.
- Le fichier contient des secrets (jetons, bonnes réponses) : il vit hors de `wwwroot`, n'est servi sous aucune route, et `data/` est déjà ignoré par Git.
- Le temps d'enregistrement sert au calcul du temps restant à la reprise (décision 2 du README, US-E11-03).
- ADR 0005 à rédiger pendant l'US : format du fichier, numéro de version et règle de compatibilité (un format inconnu n'est pas repris), sérialisation polymorphe des états de manche. Mettre à jour le tableau « Décisions » de CLAUDE.md, et CLAUDE.md pour l'option `Persistence:Directory`.
- Tests : `FakeTimeProvider`, système de fichiers dans un dossier temporaire ; écriture atomique, dernier état gagnant sous rafale, arrêt normal, échec d'écriture puis rétablissement, dossier inutilisable au démarrage, aller-retour de tous les états des `LeakSuite`.

**Hors périmètre**
- La relecture au démarrage et la reprise (US-E11-02, US-E11-03).
- L'historique des parties et l'export des scores (idée non planifiée).
