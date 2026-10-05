# E13 — Buzzer

**Phase :** 4. Buzzer et blind test
**Objectif :** un départage que les joueurs jugent juste. Le premier qui appuie gagne, d'après l'instant où son doigt a touché l'écran et non d'après l'arrivée de son message au serveur. L'épopée livre la brique commune (bouton du téléphone, arbitrage du moteur) et un premier mode qui s'en sert, les questions buzzer, jouable avant que l'audio existe. Le blind test (E15) réutilise la même brique.

## User stories

| US | Titre | Statut | Dépend de |
|---|---|---|---|
| [US-E13-01](US-E13-01-bouton-buzzer.md) | Bouton buzzer du téléphone | Terminée | — |
| [US-E13-02](US-E13-02-arbitrage.md) | Arbitrage du buzz | Terminée | — |
| [US-E13-03](US-E13-03-descripteur-questions-buzzer.md) | Descripteur d'une manche de questions buzzer | Terminée | — |
| [US-E13-04](US-E13-04-question-et-gagnant.md) | Question posée et annonce du gagnant | À faire | US-E13-01, US-E13-02, US-E13-03 |
| [US-E13-05](US-E13-05-jugement-et-revelation.md) | Jugement, réouverture du buzzer et révélation | À faire | US-E13-04 |
| [US-E13-06](US-E13-06-diagnostic-horloge.md) | Diagnostic d'horloge et flash synchronisé | À faire | — |

US-E13-02 est une US technique : elle omet la ligne « En tant que ».

## Décisions

Toutes les décisions qui bloquaient l'épopée sont prises (2026-10-05). Les valeurs numériques ci-dessous sont des valeurs de départ, ajustables sans nouvelle décision.

1. **Un mode « questions buzzer » pour livrer le buzzer :** le GM pose une question, les joueurs buzzent, le premier répond à voix haute et le GM valide ou refuse. L'arbitrage vit dans le moteur (`PartyGame.Engine/Buzzers`), à disposition de tous les modes, et le bouton dans `client/src/shared/components`. L'épopée est ainsi jouable sur de vrais appareils sans attendre l'audio. Option écartée : la brique seule, vérifiée sur le terrain par le blind test (E15).
2. **Réponse orale, jugée par le GM :** le joueur qui a la main répond à voix haute et le GM tranche depuis sa console. Le téléphone ne sert qu'à buzzer. Options écartées : une saisie sur le téléphone, et un réglage par pack.
3. **Mauvais buzz : joueur bloqué pour la question.** Après une réponse refusée, le joueur ne peut plus buzzer sur cette question, sans perdre de points, et le buzzer se rouvre aux autres. Options écartées : un blocage de quelques secondes, et une pénalité de points.
4. **Gagnant désigné par l'horodatage :** le téléphone horodate l'appui (`pointerdown`, `performance.now()`) et le convertit en heure serveur avec l'horloge synchronisée (US-E05-03). Le serveur attend une fenêtre d'arbitrage après le premier buzz reçu (250 ms, `Buzzer:ArbitrationMilliseconds`), puis désigne le buzz le plus ancien. Un horodatage invraisemblable est ramené dans des bornes (US-E13-02) : une horloge déréglée ou un message forgé ne peut pas « remonter le temps ».
5. **Question affichée sur la TV à l'ouverture du buzzer :** le GM pose la question, qui s'affiche sur la TV en même temps que le buzzer s'ouvre. Le GM peut aussi la lire à voix haute. La réponse ne s'affiche qu'à la révélation. Option écartée : une question lue seulement à voix haute, sans rien sur la TV.
6. **Flash synchronisé sans intention :** la page `/diagnostic/` fait clignoter l'écran à chaque seconde entière de l'heure serveur. Des téléphones posés côte à côte clignotent ensemble si leurs horloges sont bien alignées, sans action du GM ni message nouveau. Option écartée : un flash déclenché par le GM depuis sa console.

## Ordre de réalisation suggéré

1. US-E13-01, US-E13-02, US-E13-03 et US-E13-06, en parallèle.
2. US-E13-04, puis US-E13-05.

## Critère de sortie (phase 4)

Voir le README de [E15](../E15-mode-blind-test/README.md#critère-de-sortie-phase-4). Pour cette épopée : une manche de questions buzzer est jouée de bout en bout sur de vrais appareils (au moins un iPhone sous Safari et un Android sous Chrome), avec l'écran TV et la console GM. Les joueurs, qui buzzent en même temps, jugent le départage juste.
