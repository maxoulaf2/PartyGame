### US-E12-01 — Simulateur de joueurs

**Statut :** Prête

**En tant que** opérateur
**je veux** lancer en une commande des joueurs simulés qui rejoignent la partie et jouent comme de vrais téléphones
**afin de** tester une partie, une coupure ou une charge sans réunir dix personnes

**Critères d'acceptation**
- Étant donné un serveur lancé, quand l'opérateur exécute `dotnet run --project tools/PartyGame.Bots -- --url http://192.168.1.10:5000 --count 10`, alors dix bots rejoignent le lobby sous les pseudos « Bot 01 » à « Bot 10 », et la TV et la console GM les affichent comme des joueurs ordinaires.
- Étant donné l'option `--behavior`, quand elle est donnée, alors chaque bot suit son comportement : `random` (une proposition affichée au hasard, après un délai aléatoire), `fast` (la première proposition affichée, aussitôt), `slow` (juste avant l'échéance), `silent` (ne répond jamais), `flaky` (se déconnecte et se reconnecte au hasard, et renvoie ses intentions non acquittées). Un mélange s'écrit `--behavior random:6,flaky:2,silent:2`. Par défaut : `random`.
- Étant donné une manche de quiz, quand les bots jouent, alors ils ne s'appuient que sur leur projection `Player`, comme un téléphone : ils ne connaissent jamais la bonne réponse, et ne répondent qu'aux propositions affichées.
- Étant donné un bot qui perd sa connexion, quand elle revient, alors il reprend sa session par son jeton, et ses intentions portent un `ClientSeq` croissant : le serveur ne peut pas distinguer un bot d'un téléphone.
- Étant donné l'option `--gm-code`, quand elle est donnée, alors un bot GM fait avancer la partie seul : choix du pack (`--pack`), lancement, affichage de la question puis de chaque proposition, révélation, question et manche suivantes. Sans elle, un humain pilote la partie depuis la console GM.
- Étant donné les bots en cours d'exécution, quand la console de l'outil s'affiche, alors elle résume toutes les 5 secondes : bots connectés, intentions envoyées, rejetées, reconnexions, et délai de diffusion (entre l'envoi d'une intention et le snapshot qui la reflète : médiane et 95e centile).
- Étant donné Ctrl+C, quand l'opérateur arrête l'outil, alors les bots se déconnectent proprement et le résumé final s'affiche.
- Étant donné une URL injoignable ou un code GM refusé, quand l'outil démarre, alors il s'arrête avec un message précis, sans pile d'exception.
- Étant donné les tests, quand ils s'exécutent, alors un projet `tests/PartyGame.Bots.Tests` lance un serveur de test (`WebApplicationFactory`) et vérifie qu'un bot GM et dix bots de comportements mélangés jouent une partie complète jusqu'au classement final, et qu'un bot `flaky` ne compte jamais deux réponses à la même question.

**Comportement en cas d'erreur**
Sans objet côté joueurs, public et GM : les bots sont des joueurs ordinaires. Une erreur des bots s'affiche dans leur console, jamais sur le serveur.

**Notes techniques**
- Décision 1 du README : `tools/PartyGame.Bots` est un exécutable qui sert aussi de bibliothèque aux projets de test (`BotPlayer`, `BotGameMaster`, comportements). Il ne référence que `PartyGame.Contracts` et `Microsoft.AspNetCore.SignalR.Client`, déjà présent dans `Directory.Packages.props` : dépendance à signaler dans la PR, sans nouvel appel réseau.
- Le comportement d'un bot est une stratégie par mode, choisie selon le `type` de la vue de manche : un nouveau mode ajoute la sienne dans l'outil. Un bot face à un mode inconnu attend sans rien faire.
- Les messages sont sérialisés avec `ContractJsonOptions`, comme ceux du serveur.
- Mettre à jour CLAUDE.md : arborescence (`tools/`), sens des dépendances (`Bots` ne dépend que de `Contracts`) et section « Commandes ».

**Hors périmètre**
- Le test de charge sur le Raspberry Pi (E21), qui s'appuiera sur cet outil.
- Les comportements du buzzer (E13) et des questions ouvertes (E16), ajoutés avec leurs modes.
- Une interface graphique.
