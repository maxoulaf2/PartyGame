# ADR 0005 — Enregistrement de la partie sur le disque

**Statut :** Accepté
**Date :** 2026-10-04

## Contexte

Un crash du serveur, un PC qui redémarre ou un arrêt par erreur ne doivent coûter qu'une brève reconnexion (E11). Il faut donc retrouver l'état complet de la partie au redémarrage : joueurs et jetons, dernier `ClientSeq` de chaque joueur, scores, pack joué, identifiants des médias, manche en cours avec l'état propre à son mode.

Contraintes :

- `GameLoop` est le seul écrivain de l'état et ne doit jamais attendre le disque : une rafale de réponses ne peut pas ralentir la partie ;
- un arrêt brutal pendant une écriture ne doit jamais laisser un fichier tronqué à la place du précédent ;
- l'état d'une manche est propre à son mode (`RoundState`), et ajouter un mode ne doit nécessiter aucune modification du moteur en dehors de son enregistrement ;
- le code GM ne doit jamais être écrit sur le disque (CLAUDE.md) ;
- le fichier contient des secrets (jetons, bonnes réponses) : il ne doit être servi par aucune route.

## Décision

### Fichier

- L'état est écrit dans `current-game.json`, dans le dossier de données `Persistence:Directory` (par défaut `data` à côté de l'exécutable ; un chemin relatif l'est au dossier de l'exécutable, comme pour les packs). Ce dossier est hors de `wwwroot` et ignoré par Git.
- Le fichier contient un objet `SavedGame` : `formatVersion` (1), `savedAt` (heure serveur de l'enregistrement, via `TimeProvider`) et `game`, l'état complet sérialisé tel quel (`GameState`). Le code GM n'en fait pas partie : il n'est pas dans l'état.
- **Compatibilité :** un serveur ne reprend jamais une partie enregistrée dans un format dont il ne connaît pas la version. Toute évolution incompatible de `GameState` ou d'un état de manche incrémente `formatVersion` ; aucune migration n'est prévue, une partie interrompue ne survivant de toute façon qu'à un redémarrage dans la même soirée.

### Écriture

- Un listener de `GameLoop`, `GamePersistence`, reçoit chaque nouvel état et le dépose dans un canal borné à un élément, qui écarte l'état en attente le plus ancien. Une tâche d'écriture unique écrit le dernier état reçu : la boucle n'attend jamais le disque, une rafale ne coûte que quelques écritures, et un état plus ancien n'écrase jamais un plus récent. L'état étant immuable, aucune copie n'est nécessaire.
- Chaque écriture passe par un fichier temporaire du même dossier (`current-game.json.tmp`), vidé sur le disque, puis renommé par-dessus le fichier précédent. Un fichier temporaire abandonné est supprimé au démarrage suivant.
- `GamePersistence` est un service hébergé enregistré avant `GameLoop` : les services s'arrêtant dans l'ordre inverse, il s'arrête après la boucle et écrit son dernier état avant la sortie du processus.
- Une écriture en échec n'interrompt jamais la partie : elle est journalisée en `Error`, et un incident `PersistenceFailed` est signalé au GM une fois par série d'échecs. Le journal des incidents l'oublie à la première écriture réussie.
- Au démarrage, le dossier est créé et une écriture d'essai y est faite avant que le serveur n'écoute : un dossier inutilisable arrête le serveur avec un message qui le nomme, comme un port déjà utilisé.

### Sérialisation des états de manche

- System.Text.Json, avec les conventions du fil (`ContractJsonOptions`) : `GameState` est déjà sérialisable tel quel, ses propriétés calculées portant `[JsonIgnore]`.
- `RoundState` est polymorphe sans attribut, puisqu'il ne connaît pas les modes : `GameStateJson.CreateOptions(GameModes)` déclare comme types dérivés l'état de manche de chaque mode enregistré (`IGameMode.StateType`, fourni par `GameMode<TDescriptor, TState>`), avec le nom de son type comme discriminant (`"$type": "QuizRound"`). Enregistrer un mode dans `AddGameModes()` suffit donc à rendre ses manches enregistrables.
- Le test `GameStateJsonTests` écrit puis relit chaque état des `LeakSuite` de l'assemblage de tests du moteur, trouvées par réflexion, et compare le JSON et les projections : un nouveau mode est couvert par sa propre suite, sans test supplémentaire.

### Alternatives écartées

- **Écriture synchrone dans la boucle :** chaque transition attendrait le disque, ce qui ralentirait toute la partie sur la carte SD d'un Raspberry Pi.
- **Un discriminant par type d'activité (`quiz`) :** plus stable qu'un nom de type, mais un mode de test ou un mode sans activité déclarée dans `PartyGame.Contracts` n'en a pas. Renommer un état de manche reste rare, et c'est un changement de format que `formatVersion` signale.
- **Journal des transitions plutôt que l'état complet :** reprise plus fine, mais relecture plus lente et sensible à toute évolution du moteur. L'état complet, lui, se réécrit d'un bloc.
- **Base de données embarquée (SQLite) :** une dépendance de plus pour un seul document réécrit en entier.

## Conséquences

### Bénéfices

- Un crash ne perd au plus que les dernières millisecondes de la partie.
- Aucun mode n'a de code de persistance à écrire.
- La boucle reste le seul écrivain de l'état, sans verrou.

### Coûts et contraintes

- Renommer un type d'état de manche, ou changer la forme de `GameState`, impose d'incrémenter `formatVersion` : les parties enregistrées avant la mise à jour ne sont plus reprises.
- Le fichier contient les jetons et les bonnes réponses : quiconque a accès au disque du serveur peut les lire.
- Le catalogue des packs fait partie de l'état et est réécrit à chaque transition.
- Les timers ne font pas partie de l'état : à la reprise, chaque mode décale ses échéances du temps passé hors ligne et reprogramme ses timers (`IGameMode.ResumeRound`, abstrait dans `GameMode<TDescriptor, TState>`, US-E11-03).

### Suivi

- Tableau « Décisions » de CLAUDE.md et option `Persistence:Directory` dans ses commandes.
- La relecture du fichier et la proposition de reprise sont l'objet de US-E11-02, la reprise elle-même de US-E11-03.
