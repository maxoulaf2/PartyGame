# E10 — Erreurs invisibles

**Phase :** 3. Résilience
**Objectif :** tenir l'engagement central du projet. Un bug dans un mode, une vue qui plante ou une image introuvable ne se voient jamais sur les téléphones ni sur la TV : la partie continue, l'affichage reste propre, et seul le GM est informé, par un panneau d'incidents discret. Si une manche échoue à répétition, le GM se voit proposer de la passer.

## User stories

| US | Titre | Statut | Dépend de |
|---|---|---|---|
| [US-E10-01](US-E10-01-incidents-gm.md) | Incidents serveur signalés au GM | Terminée | — |
| [US-E10-02](US-E10-02-passer-une-manche-defaillante.md) | Passer une manche qui échoue à répétition | Terminée | US-E10-01 |
| [US-E10-03](US-E10-03-remontee-erreurs-client.md) | Remontée des erreurs des clients | Terminée | — |
| [US-E10-04](US-E10-04-vues-protegees.md) | Vues protégées et écran TV jamais vide | Terminée | US-E10-01, US-E10-03 |

## Décisions

Toutes les décisions qui bloquaient l'épopée sont prises. Les valeurs numériques ci-dessous sont des valeurs de départ, ajustables sans nouvelle décision.

1. **Passer la manche, pas l'étape du mode :** quand une même manche échoue à répétition (3 échecs), la console GM propose de passer la manche entière. Le moteur s'en charge seul, sans rien demander au mode : la manche se termine, les points déjà acquis restent, et la partie continue avec la manche suivante ou le classement final. Le quiz garde son « Passer la question » pour un souci ponctuel. Option écartée : une étape « passable » propre à chaque mode, exposée par `IGameMode`, plus fine mais à implémenter par chaque nouveau mode.
2. **Incidents hors de l'état de jeu :** un incident n'est pas un changement de la partie. Le serveur les tient dans un journal en mémoire, à part de `GameState`, et les envoie au seul GM par un message dédié, sans faire avancer la version des snapshots. Ils ne survivent pas à un redémarrage : les logs en gardent la trace. Choix de réalisation, ajustable sans nouvelle décision.
3. **Le GM n'est dérangé que s'il peut agir :** un incident isolé dont le serveur s'est remis seul n'apparaît que comme un compteur discret dans la console. La proposition de passer une manche, elle, s'affiche en évidence.

## Ordre de réalisation suggéré

1. US-E10-01 et US-E10-03, en parallèle.
2. US-E10-02 et US-E10-04, en parallèle.

## Critère de sortie (phase 3)

Porté par E12 : si on tue le serveur en pleine question, la partie reprend sans que les joueurs fassent quoi que ce soit ; si on injecte une exception, seul le GM en est informé.
