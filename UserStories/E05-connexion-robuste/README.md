# E05 — Connexion robuste

**Phase :** 1. Squelette temps réel
**Objectif :** une veille, un changement d'application, un rechargement ou une coupure Wi-Fi restent sans conséquence. Chaque client se reconnecte seul, retrouve son identité et l'état courant, et n'affiche rien d'interactif tant qu'il n'est pas resynchronisé. Les horloges des clients sont alignées sur celle du serveur, en préparation des comptes à rebours (E08) et du buzzer (E13).

## User stories

| US | Titre | Statut | Dépend de |
|---|---|---|---|
| [US-E05-01](US-E05-01-reconnexion-par-jeton.md) | Reconnexion automatique par jeton | Terminée | US-E04-02 |
| [US-E05-02](US-E05-02-indicateur-et-verrouillage.md) | Indicateur de coupure et interactions verrouillées | À faire | US-E05-01 |
| [US-E05-03](US-E05-03-synchronisation-horloge.md) | Synchronisation d'horloge avec le serveur | Terminée | US-E03-04 |
| [US-E05-04](US-E05-04-rechargement-nouvelle-version.md) | Rechargement automatique sur nouvelle version | À faire | US-E03-04 |

## Décisions

Toutes les décisions qui bloquaient l'épopée sont prises. Les paramètres techniques ci-dessous sont des valeurs de départ, ajustables sans nouvelle décision :

1. **Reconnexion sans limite :** la politique de `withAutomaticReconnect` réessaie indéfiniment (délais croissants jusqu'à 10 s), au lieu des quatre tentatives par défaut de `@microsoft/signalr`. Le retour au premier plan d'une page (`visibilitychange`) déclenche une tentative immédiate.
2. **Paramètres de synchronisation d'horloge :** salve de 8 allers-retours, estimation fondée sur les échantillons au RTT minimal, resynchronisation toutes les 60 s et à chaque reconnexion.
3. **Identifiant de build :** généré au build du front et écrit dans `wwwroot`, d'où le serveur le lit au démarrage. Le contrôle est inactif en développement (`npm run dev`).

## Ordre de réalisation suggéré

1. US-E05-01, qui porte le critère de sortie, et US-E05-03 en parallèle.
2. US-E05-02.
3. US-E05-04.

## Critère de sortie (phase 1)

Trois téléphones (au moins un iPhone sous Safari et un Android sous Chrome), l'écran TV et le GM sont réunis dans le lobby. Un téléphone mis en veille une minute revient dans le lobby sans aucune action de son utilisateur, avec son pseudo, et la TV le montre de nouveau connecté. `dotnet test`, `npm run check`, `npm run test` et `npm run e2e` passent. La vérification est faite avec le serveur lancé par `dotnet run`, sur le Wi-Fi d'un vrai réseau domestique.
