# E12 — Outillage de test et diagnostic

**Phase :** 3. Résilience
**Objectif :** prouver la résilience au lieu de l'espérer. Des bots jouent à la place de vrais joueurs, des scénarios E2E réunissent une table complète sur un serveur dédié, et des tests de chaos coupent le réseau, injectent des exceptions et tuent le serveur en pleine question. Sur place, une page de diagnostic dit en une minute si le réseau du lieu convient.

## User stories

| US | Titre | Statut | Dépend de |
|---|---|---|---|
| [US-E12-01](US-E12-01-simulateur-de-joueurs.md) | Simulateur de joueurs | Terminée | — |
| [US-E12-02](US-E12-02-table-complete-e2e.md) | Table complète sur un serveur dédié en E2E | Terminée | — |
| [US-E12-03](US-E12-03-tests-de-chaos.md) | Tests de chaos | À faire | US-E10-02, US-E10-04, US-E11-03, US-E12-01, US-E12-02 |
| [US-E12-04](US-E12-04-diagnostic-reseau.md) | Diagnostic réseau sur place | Terminée | — |

Comme l'outillage des phases 0 et 1, US-E12-02 et US-E12-03 sont des US techniques : elles omettent la ligne « En tant que ».

## Décisions

Toutes les décisions qui bloquaient l'épopée sont prises. Les valeurs numériques ci-dessous sont des valeurs de départ, ajustables sans nouvelle décision.

1. **Bots en .NET :** le simulateur est un outil `tools/PartyGame.Bots`, à la fois bibliothèque et application console, qui ne dépend que de `PartyGame.Contracts`. Il utilise `Microsoft.AspNetCore.SignalR.Client`, déjà employé par les tests d'intégration du hub : dépendance Microsoft, sans appel réseau hors du serveur visé. Les tests d'intégration, les tests de chaos et le test de charge sur le Pi (E21) s'en servent. Option écartée : un script TypeScript sous Node, qui aurait réutilisé `shared/connection` mais se prête mal à xUnit et impose Node sur le Pi.
2. **Diagnostic réseau en deux volets :** une page `/diagnostic/`, que n'importe quel téléphone ouvre sans s'inscrire, et un résumé par appareil dans la console GM. Options écartées : la console GM seule, qui ne permet aucun test avant l'arrivée des joueurs, et la page seule, qui oblige le GM à passer de téléphone en téléphone.

## Ordre de réalisation suggéré

1. US-E12-01, US-E12-02 et US-E12-04, dès le début de la phase, en parallèle de E10 et E11.
2. US-E12-03, une fois E10 et E11 terminées.

## Critère de sortie (phase 3)

Sur de vrais appareils (au moins un iPhone sous Safari et un Android sous Chrome), avec l'écran TV et la console GM, serveur lancé par `scripts/start.ps1` sur le Wi-Fi d'un vrai réseau domestique :

- on tue le processus du serveur en pleine question, on le relance, le GM saisit le nouveau code et reprend la partie : les téléphones et la TV reviennent dans la question, avec le temps qui restait, sans aucune action des joueurs ;
- on injecte une exception dans le mode (US-E12-03) : les téléphones et la TV n'affichent rien de particulier, la console GM montre l'incident puis, à la troisième, propose de passer la manche.

Les mêmes scénarios sont automatisés (US-E12-03). `dotnet test`, `dotnet format --verify-no-changes`, `npm run check`, `npm run test` et `npm run e2e` passent.
