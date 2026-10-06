# E20 — Habillage

**Phase :** 6. Une vraie soirée
**Objectif :** que la soirée ait l'air et le son d'un vrai jeu télévisé : un thème visuel soigné, des transitions fluides sur la TV, des jingles aux moments clés, et une interface lisible par tous.

## User stories

| US | Titre | Statut | Dépend de |
|---|---|---|---|
| [US-E20-01](US-E20-01-theme-et-transitions.md) | Thème visuel et transitions de l'écran TV | Terminée | US-E18-01 |
| [US-E20-02](US-E20-02-jingles-et-ambiance.md) | Jingles et sons d'ambiance | À faire | US-E18-01, US-E19-01 |
| [US-E20-03](US-E20-03-accessibilite.md) | Vérification de l'accessibilité | À faire | US-E20-01 |

## Décisions

Toutes les décisions qui bloquaient l'épopée sont prises (2026-10-06).

1. **Sons embarqués, remplaçables par le pack :** le build embarque un jeu de sons par défaut (fichiers locaux sous licence libre, CC0 de préférence, avec leur provenance documentée). Un pack peut remplacer chacun d'eux par ses propres fichiers, validés au chargement comme ses autres médias. Options écartées : des sons fournis par le pack seulement, et un jeu de sons fixe que les packs ne peuvent pas changer.
2. **Jingles déclenchés par la TV à partir du snapshot :** un jingle accompagne un changement d'écran (introduction de manche, révélation, podium), que la TV détecte dans le snapshot. Le serveur ne décrit dans la projection `Display` que les sons choisis par le pack, sous `/media/<identifiant>`. Les extraits de blind test gardent leur lecture décrite par le serveur (`AudioPlayback`) et sont prioritaires : aucun jingle ni son d'ambiance ne joue pendant un extrait. Choix de réalisation, ajustable sans nouvelle décision.
3. **Un son qui échoue ne fait pas d'incident :** le GM ne peut rien y faire en pleine soirée. L'échec est remonté au serveur et journalisé en `Warning`, et la TV continue sans le son. Les sons d'un pack sont de toute façon vérifiés au chargement. Choix de réalisation, ajustable sans nouvelle décision.
4. **Police auto-hébergée :** une police sous licence libre (OFL), en fichiers `woff2` dans `client/src/assets/fonts/`, remplace les polices système sur les trois pages. Choix de réalisation, ajustable sans nouvelle décision.
5. **Accessibilité vérifiée sans nouvelle dépendance :** les contrastes du thème sont vérifiés par un test Vitest qui calcule les ratios WCAG à partir des variables de `theme.css`. L'ajout d'un outil d'audit (axe-core) serait une nouvelle dépendance npm, à proposer à part. Choix de réalisation, ajustable sans nouvelle décision.

## Ordre de réalisation suggéré

1. US-E20-01, après l'introduction des manches (US-E18-01) et les classements animés (US-E18-02).
2. US-E20-02, après la pause (US-E19-01), qui doit aussi couper les jingles.
3. US-E20-03 en dernier, sur le thème définitif.

## Critère de sortie (phase 6)

Une soirée test d'une heure, avec un pack d'au moins trois modes, est menée par le GM depuis sa console sans toucher au PC hôte : introduction de chaque manche, au moins une pause, un ajustement de score, une manche déplacée ou retirée, puis le podium final. Elle est jouée sur de vrais appareils (au moins un iPhone sous Safari et un Android sous Chrome), avec l'écran TV et la console GM, serveur lancé par `scripts/start.ps1`.

`dotnet test`, `dotnet format --verify-no-changes`, `npm run check`, `npm run test` et `npm run e2e` passent.
