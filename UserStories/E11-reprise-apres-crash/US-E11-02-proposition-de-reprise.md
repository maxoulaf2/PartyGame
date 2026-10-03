### US-E11-02 — Proposition de reprise au GM

**Statut :** À faire

**En tant que** game master
**je veux** qu'au redémarrage du serveur la console me propose de reprendre la partie interrompue ou d'en commencer une nouvelle
**afin de** décider moi-même, en sachant ce que je reprends

**Critères d'acceptation**
- Étant donné un fichier `current-game.json` lisible, d'un format connu, avec au moins un joueur, quand le serveur démarre, alors la partie n'est pas reprise d'office : le serveur attend la décision du GM, et la bannière de la console indique qu'une partie interrompue est en attente.
- Étant donné une console GM dont le code est accepté, quand une reprise est en attente, alors elle affiche la partie trouvée : titre du pack, avancement (« Lobby », « Manche 2/3 · Finale · Question 4/10 », « Partie terminée »), nombre de joueurs, heure de l'enregistrement, et deux boutons : « Reprendre la partie » et « Nouvelle partie ».
- Étant donné une console GM qui avait mémorisé le code du démarrage précédent, quand elle se reconnecte, alors ce code est refusé et elle redemande le code, avec un texte qui explique que le serveur a redémarré et que le nouveau code est dans sa console (décision 3 du README).
- Étant donné la reprise en attente, quand un téléphone se reconnecte avec son jeton, alors il reste sur « Retour dans la partie… », sans formulaire, jusqu'à la décision du GM. Un téléphone sans jeton affiche l'écran d'attente neutre, sans formulaire.
- Étant donné la reprise en attente, quand l'écran TV se connecte, alors il affiche un écran d'attente habillé (« Reprise de la partie… »), jamais un écran vide.
- Étant donné les médias du pack de la partie trouvée, quand la reprise est proposée, alors leur présence sur le disque est vérifiée. S'il en manque, « Reprendre la partie » est désactivé et la console liste les fichiers manquants, avec un bouton « Vérifier de nouveau » : une partie reprise ne doit jamais échouer à cause de son contenu.
- Étant donné « Nouvelle partie », quand le GM confirme (« Les joueurs devront se réinscrire »), alors l'ancien fichier est renommé `previous-game.json` (en remplaçant un éventuel précédent), une nouvelle partie démarre dans le lobby avec le catalogue des packs relu, et les téléphones en attente affichent le formulaire d'inscription prérempli avec leur dernier pseudo (`SessionUnknown`, US-E05-01).
- Étant donné un fichier illisible, tronqué ou d'un format inconnu, quand le serveur démarre, alors il le renomme `current-game.unreadable-<horodatage>.json`, journalise la raison en `Warning`, démarre une nouvelle partie, et signale au GM un incident `SavedGameUnreadable` : il sait que les joueurs devront se réinscrire.
- Étant donné un fichier sans joueur ou absent, quand le serveur démarre, alors une nouvelle partie démarre normalement, sans proposition.
- Étant donné deux consoles GM, quand l'une décide, alors l'autre voit aussitôt la partie reprise ou la nouvelle partie. Une seconde décision qui arrive ensuite est rejetée comme obsolète.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent : démarrage avec un fichier valide, sans joueur, illisible, d'un format inconnu ; reprise acceptée ; nouvelle partie ; médias manquants puis rétablis ; double décision ; ancien code GM refusé ; téléphones avec et sans jeton pendant l'attente.

**Comportement en cas d'erreur**
Joueurs et public : des écrans d'attente, jamais de message technique. GM : la description de la partie trouvée, les médias manquants, ou l'incident `SavedGameUnreadable`.

**Notes techniques**
- La décision est une intention GM, `ResolveSavedGame(savedGameId, resume)`, qui nomme la partie trouvée pour rester idempotente. Le choix entre la partie enregistrée et une nouvelle partie se fait avant de créer l'état initial de `GameLoop` ; une piste est une phase `ResumePending` dont l'état ne contient que le résumé de la partie trouvée, la partie elle-même étant chargée à la décision. À préciser au début de l'US.
- Pendant l'attente, le hub répond un nouveau refus `GamePending` à `ResumeSession` et à `JoinGame`, sans oublier le jeton. La décision est annoncée à toutes les connexions, qui relancent alors leur démarche (reprise par jeton ou formulaire).
- La projection `GameMaster` porte le résumé de la partie trouvée ; les projections `Player` et `Display` n'en contiennent rien (test de non-fuite).
- La vérification des médias réutilise celle du chargement des packs (US-E06-02), appliquée aux chemins de `PackMedia` enregistrés.
- Textes dans `fr.ts` ; mise à jour de [docs/installation.md](../../docs/installation.md) (reprise après crash, fichiers de `data/`).

**Hors périmètre**
- La reprise elle-même, comptes à rebours compris (US-E11-03).
- Plusieurs parties enregistrées au choix.
- « Nouvelle partie » en cours de soirée, sans redémarrage (E19).
