# E19 — Contrôles avancés du GM

**Phase :** 6. Une vraie soirée
**Objectif :** que le GM garde la main sur la soirée quoi qu'il arrive, sans toucher au PC hôte. Il peut mettre la partie en pause, corriger un score et changer le programme en cours de route.

## Déjà en place

- **Nouvelle partie sans redémarrer le serveur :** le retour au lobby, possible à tout moment, garde les joueurs inscrits et remet les scores à zéro (`ReturnToLobby`).
- **Arrivées en cours de partie :** les inscriptions restent ouvertes, et un joueur arrivé en cours de partie joue dès la prochaine question ouverte, avec 0 point (décision 5 du README de E08, décision 4 du README de E09). La décision 2 ci-dessous rend cette règle définitive.
- **Passer la manche en cours :** l'intention `SkipRound` existe (US-E10-02), mais la console ne la propose que pour une manche défaillante.

## User stories

| US | Titre | Statut | Dépend de |
|---|---|---|---|
| [US-E19-01](US-E19-01-pause-et-reprise.md) | Pause et reprise de la partie | Terminée | — |
| [US-E19-02](US-E19-02-ajustement-des-scores.md) | Ajustement manuel des scores | À faire | — |
| [US-E19-03](US-E19-03-programme-des-manches.md) | Saut et réordonnancement des manches | À faire | US-E18-01 |

## Décisions

Toutes les décisions qui bloquaient l'épopée sont prises (2026-10-06).

1. **Pause en gel complet :** pendant la pause, les comptes à rebours et l'audio de la TV s'arrêtent, et toute intention de manche est refusée, des joueurs comme du GM. La TV et les téléphones affichent « Pause ». À la reprise, chaque compte à rebours repart du temps qui lui restait, et l'extrait reprend là où il s'était arrêté. Option écartée : un gel accompagné du classement sur la TV.
2. **Retardataires à 0 point, sans rattrapage automatique :** la règle provisoire de E08 et E09 devient définitive. Un joueur arrivé en cours de partie participe à toute question dont les réponses s'ouvrent après son inscription et commence à 0 point. Le GM peut lui donner des points par l'ajustement manuel des scores (US-E19-02). Options écartées : démarrer avec le score du dernier, et un choix du GM à chaque arrivée.
3. **Intentions GM par comparaison et échange :** comme les intentions qui nomment l'étape qu'elles font avancer (décision 1 du README de E07), un ajustement de score nomme le score qu'il corrige et un réordonnancement nomme l'ordre qu'il remplace. Une intention renvoyée ou envoyée par une seconde console qui ne voit pas le dernier état est rejetée comme obsolète. Choix de réalisation, ajustable sans nouvelle décision.
4. **Pas d'exclusion de joueur :** la décision 4 du README de E04 reste valable.

## Ordre de réalisation suggéré

1. US-E19-01 et US-E19-02, indépendantes.
2. US-E19-03 après l'introduction des manches (US-E18-01), dont elle reprend le titre et le mode dans la liste du programme.

## Critère de sortie (phase 6)

Voir le README de [E20](../E20-habillage/README.md#critère-de-sortie-phase-6).
