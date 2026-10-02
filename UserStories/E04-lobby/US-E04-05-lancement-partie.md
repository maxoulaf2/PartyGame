### US-E04-05 — Lancement de la partie

**Statut :** Terminée

**En tant que** game master
**je veux** lancer la partie quand les joueurs sont prêts
**afin que** tous les écrans passent ensemble du lobby au jeu

**Critères d'acceptation**
- Étant donné le lobby sans aucun joueur, quand le GM regarde le bouton « Lancer la partie », alors il est désactivé, avec une indication du nombre de joueurs requis (au moins 1).
- Étant donné au moins un joueur inscrit, quand le GM appuie sur « Lancer la partie », alors une confirmation lui est demandée, puis la partie passe en phase `Started`.
- Étant donné la partie lancée, quand les snapshots sont diffusés, alors la TV affiche « La partie commence », chaque téléphone affiche un écran d'attente correspondant, et l'interface GM affiche l'état « Partie en cours » : ces écrans provisoires seront remplacés par la première manche en E07.
- Étant donné la partie déjà lancée, quand une seconde intention de lancement arrive (double appui, deuxième GM), alors elle est rejetée sans effet.
- Étant donné la partie lancée, quand un nouveau téléphone rejoint (décision 3 du README), alors il est inscrit et voit l'écran d'attente de la partie en cours ; la TV et le GM le voient apparaître dans la liste des joueurs.
- Étant donné les tests du moteur, quand ils s'exécutent, alors ils couvrent le lancement, le refus sans joueur et le refus d'un double lancement. Un test E2E lance la partie et vérifie les trois interfaces.

**Comportement en cas d'erreur**
GM : connexion perdue, le bouton est désactivé jusqu'au snapshot frais suivant (US-E05-02). Une intention perdue pendant la coupure n'a aucun effet : le snapshot reçu à la reconnexion montre que la partie n'est pas lancée, et le GM appuie de nouveau. Le renvoi automatique des intentions non acquittées arrive en E08. Joueurs et public : ils passent à l'écran suivant à la réception du snapshot, sans message.

**Notes techniques**
- Intention GM `StartGame`. La phase `Started` est provisoire : E07 la remplacera par le déroulé des manches du pack.
- La confirmation est une simple boîte de dialogue de l'interface GM (composant Svelte, pas `window.confirm`, mal rendu sur mobile).
- Le minimum d'un joueur facilite les tests ; une valeur plus haute pourra être configurée plus tard.
- Tous les textes sont dans `fr.ts`.
- Réalisation : contrats `StartGameResult(refusal)` et codes `StartGameRefusal` : `NotEnoughPlayers`, `AlreadyStarted`, `StartFailed` (bug du moteur). `GameMasterSnapshot` porte `MinimumPlayerCount`, pour que la console dérive l'état du bouton du snapshot sans dupliquer la règle ; la valeur vient de `Lobby/Launch.MinimumPlayerCount` (1), compté sur les joueurs inscrits, connectés ou non.
- Réalisation : le hub expose `StartGame`, marqué `[GameMasterOnly]` et sans message (rien à transmettre avant le `ClientSeq` de E08) : sans authentification, l'intention est ignorée et la réponse est vide. L'intention moteur `StartGame` est traitée par `Lobby/Launch` : refusée hors du lobby (`GameAlreadyStarted`) ou sans assez de joueurs (`NotEnoughPlayers`), sinon la phase passe à `Started`. La boucle traitant les entrées une à une, deux lancements simultanés n'en acceptent qu'un. Journalisation `Information` (« Game started by the game master with {PlayerCount} players »).
- Réalisation : côté client, `GameMasterSession.startGame` envoie l'intention et traduit la réponse (`started`, code de refus, ou `unreachable`). `gm/StartControl.svelte`, sous les compteurs de la console, affiche « Lancer la partie », désactivé sans assez de joueurs (avec le minimum requis) ou tant que la connexion n'est pas rétablie et le snapshot rafraîchi ; il ouvre `shared/components/ConfirmDialog.svelte`, un `<dialog>` natif modal (Échap, « Annuler » ou un appui hors de la boîte l'annulent). Seul `StartFailed` affiche un message ; les autres refus se lisent dans le snapshot. Une fois lancée, la console affiche « Partie en cours » à la place du bouton, et garde la liste et le renommage.
- Réalisation : la TV garde l'écran du lobby (QR code et liste, les inscriptions restant ouvertes) et affiche « La partie commence ! » sous le titre. Le téléphone affiche le texte déjà prévu pour la phase `Started`, y compris pour un joueur arrivé après le lancement.
- Réalisation : tests du moteur (`LaunchTests` : lancement, joueurs tous déconnectés, refus sans joueur, refus d'un double lancement, inscription après le lancement ; `SnapshotsTests` : minimum transmis au GM), tests d'intégration du hub (`StartGameTests` : diffusion de la phase aux trois rôles, refus sans joueur et double lancement sans diffusion, deux lancements simultanés, arrivée après le lancement, intention ignorée sans authentification GM ou en tant que TV), Vitest (`gameMasterSession.test.ts`) et Playwright : `e2e/gm.spec.ts` (bouton désactivé sans joueur, avec un faux hub), `e2e/display.spec.ts` (20 joueurs lisibles et dans la zone sûre, aussi en phase `Started`) et `e2e/launch.spec.ts` (annulation, lancement, puis vérification de la console, de la TV, du téléphone et d'un téléphone arrivé ensuite). Lancer la partie est irréversible sur le serveur partagé par les tests E2E : ce fichier tourne dans un projet Playwright `launch`, qui dépend des trois autres et passe donc en dernier.
- Réalisation : la vérification sur de vrais appareils (iPhone et Android) reste à faire.

**Hors périmètre**
- Choix du pack et première manche (E06, E07).
- Retour au lobby, pause et fin de partie (E19).
- Points de départ et participation à une manche d'un joueur arrivé en cours de partie (E08, E09).
