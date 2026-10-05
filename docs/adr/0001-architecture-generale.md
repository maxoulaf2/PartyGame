# ADR 0001 — Architecture générale

**Statut :** Accepté
**Date :** 2026-10-01

## Contexte

PartyGame fait jouer plusieurs personnes en même temps, chacune sur le navigateur de son téléphone, pendant qu'une TV affiche l'état du jeu et qu'un game master (GM) pilote la partie. Tout tourne sur le réseau local du lieu, sans aucune dépendance à Internet. Plusieurs contraintes pèsent sur l'architecture.

**Des appels concurrents sur un état partagé.** SignalR exécute les méthodes du hub en parallèle, une par connexion. Lors d'un buzzer ou de la fin d'une question, des dizaines d'intentions arrivent dans la même fraction de seconde, auxquelles s'ajoutent les timers (fin du compte à rebours, fenêtre d'arbitrage). Contrairement à une API REST, toutes ces actions modifient le même état : la partie en cours. Sans précaution, on s'expose à des courses critiques difficiles à reproduire, précisément dans les moments où l'équité compte le plus.

**Un réseau local instable.** Le Wi-Fi d'une salle des fêtes ou d'un salon est saturé, sujet aux pertes de paquets et aux coupures brèves. Un message peut se perdre, arriver en double ou en retard. Le client ne peut pas supposer qu'il a reçu toutes les mises à jour.

**Des téléphones mis en veille.** Un joueur verrouille son téléphone entre deux manches, change d'application ou recharge la page. La connexion WebSocket est alors coupée, parfois pendant plusieurs minutes, et le navigateur peut perdre tout son état en mémoire. Au retour, le téléphone doit afficher l'état exact de la partie, sans action du joueur.

**Aucune fuite d'information.** N'importe qui sur le réseau peut ouvrir l'écran TV dans son navigateur ou inspecter les messages WebSocket reçus par son téléphone. Si la bonne réponse ou le choix d'un autre joueur transite vers un client avant la révélation, la triche est triviale.

**Un hôte modeste.** Le serveur tourne sur un PC portable, et à terme sur un Raspberry Pi. Il doit rester simple à lancer, sans base de données ni service externe, et pouvoir reprendre une partie après un crash.

**Des tests fiables.** Le buzzer et les comptes à rebours dépendent du temps. Le mélange des propositions dépend du hasard. Sans maîtrise de ces deux sources, les tests seraient lents et non reproductibles.

## Décision

### Serveur autoritaire

Le serveur détient seul l'état de la partie. Il calcule les scores, valide les réponses et tranche les buzzers. Les clients n'envoient que des intentions (`SubmitAnswer`, `Buzz`, `NextStep`…) et n'embarquent aucune logique de jeu : ce sont des vues du dernier snapshot reçu.

### Une boucle unique par partie

Chaque partie possède une file `Channel<GameInput>` et une boucle unique, `GameLoop`, exécutée dans un `BackgroundService`, qui traite les entrées une par une.

- Les méthodes du hub ne touchent jamais à l'état : elles valident la forme du message, l'enrichissent (joueur, heure de réception) et le déposent dans la file.
- Les timers ne modifient pas l'état eux-mêmes : à échéance, ils déposent une entrée `TimerElapsed` dans la file.
- `GameInput` regroupe les intentions des clients et les événements internes (`TimerElapsed`, `MediaFailed`…).
- La boucle est le seul écrivain de l'état : aucun `lock` ni collection concurrente dans le moteur.

### Un moteur pur

Le moteur (`PartyGame.Engine`) est une fonction pure, sans entrée/sortie, sans horloge et sans hasard propre :

```csharp
public Transition Handle(GameState state, GameInput input, GameContext context);

public sealed record Transition(GameState State, ImmutableArray<Effect> Effects);
```

- L'état est immuable : des `record` avec `ImmutableArray` et `ImmutableDictionary`, mis à jour avec `with`.
- Le temps (`context.Now`) et le hasard (`context.Random`, à graine contrôlée) sont fournis par le contexte. Côté serveur, le temps provient d'un `TimeProvider` injecté.
- Les effets (`ScheduleTimer`, `CancelTimer`…) décrivent ce qui doit se passer hors du moteur. `GameLoop`, dans `PartyGame.Server`, les exécute.
- L'audio n'est pas un effet : la projection `Display` d'un mode décrit la lecture en cours (`AudioPlayback` : média, position, instant de déclenchement en heure serveur ou position de pause), dont l'écran TV déduit à tout moment ce qu'il doit jouer, y compris après un rechargement (décision 2 de E14, US-E14-03).
- Une intention invalide n'est pas une exception : `Handle` retourne la même instance d'état sans effet, et rien n'est diffusé.
- `PartyGame.Engine` ne dépend que de `PartyGame.Contracts` et ne référence jamais ASP.NET Core ni SignalR. Un test d'architecture le vérifie.

### Des snapshots complets, versionnés et projetés par rôle

Après chaque transition qui change l'état, le serveur envoie à chaque client l'état complet qui le concerne, et non une différence.

- Chaque snapshot porte un numéro de `Version` croissant. Un client ignore toute version inférieure ou égale à celle qu'il affiche.
- L'état du moteur n'est jamais sérialisé directement : chaque mode fournit une projection explicite vers des DTO de `PartyGame.Contracts`, une par rôle (`Player`, `Display`, `GameMaster`).
- Seule la projection `GameMaster` contient les bonnes réponses avant la révélation. Une projection `Player` ne contient jamais les réponses des autres joueurs avant la révélation. Chaque couple phase/rôle est couvert par un test de non-fuite.
- La projection `Display` est envoyée au groupe `display`, la projection `GameMaster` au groupe `gm`, et chaque joueur reçoit sa propre projection `Player`.
- Un joueur est identifié par un jeton généré par le serveur et stocké dans le `localStorage` de son téléphone, jamais par un `ConnectionId` SignalR. À la reconnexion, il présente son jeton et reçoit le snapshot courant.

### Hébergement sur PC, Raspberry Pi en cible secondaire

Un unique processus ASP.NET Core sert le front (fichiers statiques) et le hub SignalR. La cible principale est un PC sous Windows ; un Raspberry Pi (`linux-arm64`, publication autonome) est une cible secondaire, validée en phase 7. Aucune base de données : l'état est persisté dans un fichier (`data/current-game.json`), écrit de façon atomique après chaque transition.

### Alternatives écartées

- **Verrous autour d'un état partagé mutable.** Chaque méthode du hub prendrait un `lock` (ou un `SemaphoreSlim`) avant de modifier l'état. Rejeté : la correction repose sur la discipline de chaque développeur, un oubli produit une course critique silencieuse, les timers ajoutent des chemins concurrents supplémentaires, et l'ordre de traitement des buzzers devient implicite. La file rend cet ordre explicite et testable.
- **État mutable dans le moteur.** Plus familier, mais un mode qui lève une exception à mi-traitement laisse l'état à moitié modifié. Il faudrait alors un mécanisme de retour arrière dédié, et la persistance comme les tests imposeraient des copies défensives. L'immuabilité rend tout cela gratuit.
- **Diffusion de différences (diffs).** Plus économe en bande passante, mais un client qui manque un seul diff (veille, paquet perdu, reconnexion) affiche un état faux jusqu'à une resynchronisation complète, qu'il faut de toute façon implémenter. Les snapshots complets rendent la reconnexion identique au fonctionnement normal. Pour quelques dizaines de joueurs et des états de quelques kilo-octets, le surcoût est négligeable.
- **Logique de jeu côté client.** Réduirait la latence perçue, mais rendrait la triche triviale et dupliquerait les règles entre serveur et client. L'interface optimiste (choix affiché « en attente ») suffit à masquer la latence.
- **Un même snapshot pour tous, filtré à l'affichage.** Rejeté : toute donnée envoyée à un client est lisible par son utilisateur. Le filtrage doit avoir lieu côté serveur.

## Conséquences

### Bénéfices

- **Pas de verrous.** La boucle est le seul écrivain : aucune course critique possible sur l'état, et le code des modes reste séquentiel et lisible.
- **Tests déterministes.** Le moteur se teste en Given/When/Then sur des fonctions pures, avec `FakeTimeProvider` et un hasard à graine fixe, sans réseau ni attente réelle.
- **Retour arrière gratuit.** Si un mode lève une exception, `GameLoop` conserve l'état précédent, journalise l'erreur et signale un incident au GM. Un bug dans une manche n'interrompt pas la partie.
- **Persistance simple.** L'état immuable se sérialise tel quel après chaque transition, ce qui permet de reprendre une partie après un crash.
- **Reconnexion triviale.** Un client qui revient reçoit le snapshot courant : il n'y a pas d'historique à rejouer.
- **Sécurité par construction.** Les projections explicites et leurs tests de non-fuite empêchent qu'une donnée secrète atteigne un client par inadvertance.
- **Modes isolés.** Un mode est une machine à états plus ses projections, ajoutée sans modifier le moteur.

### Coûts et contraintes

- **Des snapshots plus lourds que des diffs.** Chaque changement renvoie l'état complet à chaque client. Le volume reste à surveiller lors du test de charge sur Raspberry Pi (E21), en particulier pour les modes aux états volumineux.
- **Tout passe par la file.** Même une lecture simple ou un événement interne doit transiter par `GameLoop`. Une entrée lente à traiter retarde toutes les suivantes : le traitement d'une entrée doit rester rapide, et les entrées/sorties (médias, persistance) sont déléguées aux effets.
- **Une projection à écrire par rôle et par mode,** avec ses tests de non-fuite. C'est le prix de l'absence de fuite.
- **Allocations dues à l'immuabilité.** Chaque transition crée de nouvelles instances. Négligeable à l'échelle d'une partie locale.
- **Un seul processus hôte.** Pas de répartition sur plusieurs serveurs. C'est cohérent avec le périmètre local ; l'hébergement en ligne est hors périmètre.

### Suivi

- Le tableau « Décisions » de CLAUDE.md renvoie vers cet ADR pour les lignes « Serveur .NET + SignalR, état autoritaire, snapshots par rôle » et « Hôte sur PC, Raspberry Pi en cible secondaire ».
- Les règles détaillées qui en découlent sont tenues à jour dans `docs/coding-guidelines.md` (sections « Architecture serveur » et « Gestion des erreurs »).
- La mise en œuvre est portée par l'épopée E03 (boucle de jeu et diffusion) et vérifiée en charge par E21.
