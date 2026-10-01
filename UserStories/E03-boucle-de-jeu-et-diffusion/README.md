# E03 — Boucle de jeu et diffusion

**Phase :** 1. Squelette temps réel
**Objectif :** la chaîne complète intention → file → moteur → snapshot → affichage fonctionne de bout en bout, sans verrou et sans fuite d'information, conformément à l'[ADR 0001](../../docs/adr/0001-architecture-generale.md). Les épopées E04 et E05 s'appuient sur elle pour livrer le lobby.

## User stories

| US | Titre | Statut | Dépend de |
|---|---|---|---|
| [US-E03-01](US-E03-01-moteur-pur.md) | Moteur pur : état immuable, transitions et effets | Terminée | — |
| [US-E03-02](US-E03-02-game-loop.md) | `GameLoop` : une file et un seul écrivain | Terminée | US-E03-01 |
| [US-E03-03](US-E03-03-timers.md) | Timers déposés dans la file | Prête | US-E03-02 |
| [US-E03-04](US-E03-04-hub-roles-groupes.md) | Hub SignalR typé, rôles et groupes | Prête | US-E03-02 |
| [US-E03-05](US-E03-05-snapshots-par-role.md) | Snapshots versionnés, projetés par rôle et diffusés | À faire | US-E03-04 |

## Décisions

Toutes les décisions qui bloquaient l'épopée sont prises.

1. **Authentification du GM auprès du hub :** le GM saisit le code une fois. La connexion est marquée GM après vérification, et le code est vérifié de nouveau à chaque reconnexion. Aucune limitation du nombre de tentatives n'est mise en place : le réseau est celui d'une soirée privée, et le code change à chaque démarrage. Options écartées : un blocage temporaire après plusieurs échecs, jugé superflu ; le code joint à chaque intention GM, qui le ferait circuler à chaque message.
2. **Identifiant de partie dans les snapshots :** chaque snapshot porte, en plus de sa `Version`, l'identifiant de la partie (`GameId`). Un client qui reçoit un `GameId` différent de celui qu'il affiche repart de zéro au lieu d'ignorer des versions qui recommencent à 1 après un redémarrage du serveur. La reprise après crash (E11) conservera le `GameId` et la version.
3. **Dépendances ajoutées :** `@microsoft/signalr` côté client (prévu par la stack, aucun appel réseau hors du serveur auquel il se connecte) et `Microsoft.AspNetCore.SignalR.Client` dans les tests du serveur. Elles sont signalées et justifiées dans le commit qui les introduit.

## Ordre de réalisation suggéré

1. US-E03-01, qui ne dépend que de `PartyGame.Contracts`.
2. US-E03-02, puis US-E03-03 et US-E03-04 en parallèle.
3. US-E03-05, qui ferme la chaîne et débloque E04.

## Critère de sortie (phase 1)

Porté par E05 : trois téléphones, l'écran TV et le GM sont réunis dans le lobby, et un téléphone mis en veille une minute revient dans le lobby sans aucune action de son utilisateur.
