# E15 — Mode blind test

**Phase :** 4. Buzzer et blind test
**Objectif :** le blind test, pilier des soirées. La TV joue un extrait, les joueurs buzzent, la musique s'arrête net, le plus rapide répond à voix haute et le GM juge le titre et l'artiste séparément. Après un mauvais buzz, la musique reprend pour les autres. Le mode assemble le buzzer (E13) et l'audio (E14).

## User stories

| US | Titre | Statut | Dépend de |
|---|---|---|---|
| [US-E15-01](US-E15-01-descripteur-blind-test.md) | Descripteur d'une manche de blind test | Terminée | US-E14-02 |
| [US-E15-02](US-E15-02-ecoute-et-buzz.md) | Écoute de l'extrait et buzz | À faire | US-E13-01, US-E13-02, US-E14-03, US-E15-01 |
| [US-E15-03](US-E15-03-jugement-et-reprise.md) | Jugement du titre et de l'artiste, reprise après un buzz | À faire | US-E15-02 |
| [US-E15-04](US-E15-04-revelation-et-points.md) | Révélation et points | À faire | US-E15-03 |

## Décisions

Toutes les décisions qui bloquaient l'épopée sont prises (2026-10-05). Les valeurs numériques ci-dessous sont des valeurs de départ, ajustables sans nouvelle décision.

1. **Réponse orale, jugée par le GM :** comme pour les questions buzzer (décision 2 du README de E13). Options écartées : une saisie sur le téléphone, comparée par le GM ou par le serveur avec une tolérance, et un réglage par pack.
2. **Points séparés pour le titre et l'artiste :** le GM juge le titre et l'artiste séparément, et chacun rapporte ses points (`titlePoints` et `artistPoints`, 500 par défaut). Un élément trouvé n'est plus à prendre ; celui qui reste se joue entre les autres joueurs. Un morceau sans artiste dans le descripteur ne se joue que sur le titre. Option écartée : une seule bonne réponse par extrait.
3. **Un buzz par joueur et par extrait :** un joueur dont le buzz n'a rien trouvé est bloqué pour l'extrait, sans perdre de points. Un joueur qui n'a trouvé qu'un élément garde ses points mais ne rebuzze pas non plus : il a eu sa chance de donner les deux. Options écartées : un blocage de quelques secondes, et une pénalité de points.
4. **Musique en pause pendant la réponse :** au buzz (dès la désignation du gagnant), la musique s'arrête. Après le jugement, si quelque chose reste à trouver et qu'un joueur peut encore buzzer, elle reprend où elle s'était arrêtée et le buzzer se rouvre. Le temps passé en pause ne raccourcit pas l'extrait.
5. **Fin de l'extrait :** quand l'extrait est fini, la musique s'arrête mais le buzzer reste ouvert, jusqu'à ce que le GM révèle la réponse. Choix de réalisation, ajustable sans nouvelle décision.

## Ordre de réalisation suggéré

1. US-E15-01, dès que US-E14-02 est terminée.
2. US-E15-02 avec US-E14-03, puis US-E15-03 et US-E15-04.

## Critère de sortie (phase 4)

Un blind test de 10 extraits est joué dans une vraie pièce sur de vrais appareils (au moins un iPhone sous Safari et un Android sous Chrome), avec l'écran TV branché sur des enceintes et la console GM, serveur lancé par `scripts/start.ps1` sur le Wi-Fi d'un vrai réseau domestique. Les joueurs jugent le départage juste. La dispersion des horloges, mesurée avec le flash synchronisé (US-E13-06), est consignée dans `docs/mesures/dispersion-horloge.md`.

`dotnet test`, `dotnet format --verify-no-changes`, `npm run check`, `npm run test` et `npm run e2e` passent.
