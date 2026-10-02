# E04 — Lobby

**Phase :** 1. Squelette temps réel
**Objectif :** les joueurs rejoignent la partie avec un pseudo depuis leur téléphone, l'écran TV les accueille, et le GM, authentifié par son code, voit la liste, corrige un pseudo et lance la partie. C'est la première tranche verticale qui traverse toute la chaîne construite en E03.

## User stories

| US | Titre | Statut | Dépend de |
|---|---|---|---|
| [US-E04-01](US-E04-01-acces-gm.md) | Accès à l'interface GM par le code | Terminée | US-E03-05 |
| [US-E04-02](US-E04-02-rejoindre-avec-pseudo.md) | Rejoindre la partie avec un pseudo | Terminée | US-E03-05 |
| [US-E04-03](US-E04-03-lobby-ecran-tv.md) | Lobby sur l'écran TV | Terminée | US-E04-02 |
| [US-E04-04](US-E04-04-liste-et-renommage-gm.md) | Liste des joueurs et renommage par le GM | À faire | US-E04-01, US-E04-02 |
| [US-E04-05](US-E04-05-lancement-partie.md) | Lancement de la partie | À faire | US-E04-04 |
| [US-E04-06](US-E04-06-choix-interface-reseau.md) | Choix de l'interface réseau par le GM | À faire | US-E04-01, US-E04-03 |

## Décisions

Toutes les décisions qui bloquaient l'épopée sont prises.

1. **Règles du pseudo :** de 1 à 16 caractères visibles (graphèmes), après suppression des espaces en début et en fin et réduction des espaces multiples. Les émojis sont acceptés ; les caractères de contrôle et les caractères invisibles sont refusés. Deux pseudos sont en doublon s'ils sont égaux sans tenir compte de la casse ni des accents (« Zoé » et « zoe »). Options écartées : 2 à 12 caractères limités aux lettres et chiffres, trop restrictif ; doublons exacts seulement, source de confusion à l'écran.
2. **Mémorisation du code GM :** le code saisi est conservé dans le `localStorage` de l'appareil du GM et présenté à chaque connexion. S'il est refusé (serveur redémarré avec un nouveau code), l'écran de saisie réapparaît. Aucune limitation du nombre de tentatives (décision 1 de [E03](../E03-boucle-de-jeu-et-diffusion/README.md)).
3. **Arrivée après le lancement :** les inscriptions restent ouvertes pendant toute la partie. Un téléphone qui arrive après le lancement rejoint avec un pseudo et reçoit le snapshot courant. La façon dont un tel joueur entre dans une manche en cours et ses points de départ seront définis avec les modes et les scores (E08, E09) ; la question est reprise dans les points à trancher de la phase 6. Option écartée : fermer les inscriptions au lancement.
4. **Pas d'exclusion de joueur :** le GM peut renommer un joueur mais pas l'exclure, ni dans le lobby ni en cours de partie (la mention est retirée de E19). Un joueur parti reste dans la liste, affiché comme déconnecté. Options écartées : exclusion avec révocation du jeton, bannissement pour la soirée.

## Ordre de réalisation suggéré

1. US-E04-01 et US-E04-02, en parallèle.
2. US-E04-03 et US-E04-04.
3. US-E04-05, puis US-E04-06.

## Critère de sortie (phase 1)

Porté par E05 : trois téléphones, l'écran TV et le GM sont réunis dans le lobby, et un téléphone mis en veille une minute revient dans le lobby sans aucune action de son utilisateur.
