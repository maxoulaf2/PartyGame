# Roadmap

Ce document découpe le projet en phases et en épopées. Il sert de base à l'écriture des user stories au fil du développement : au début de chaque phase, on rédige les US de ses épopées à partir des pistes listées ici, puis on met à jour les statuts.

## Principes de découpage

- **Chaque phase se termine par quelque chose de démontrable.** Son critère de sortie est vérifiable sur de vrais appareils, pas seulement en test.
- **Les risques d'abord.** Le temps réel, la reconnexion et la précision du buzzer sont validés tôt, avant d'investir dans le contenu et l'habillage.
- **Des tranches verticales.** Une US traverse serveur et client si nécessaire : elle livre un comportement observable, pas une couche technique isolée. Les phases 0 et 1 font exception, avec des US techniques (voir le modèle en fin de document).
- **Les points « à trancher » se décident au plus tard au début de la phase concernée.** Chaque décision fait l'objet d'un ADR et met à jour le tableau « Décisions » de CLAUDE.md.

## Vue d'ensemble

| Phase | Objectif | Épopées | Statut |
|---|---|---|---|
| 0. Socle technique | Un dépôt outillé, un téléphone qui accède au serveur | E01, E02 | Terminée |
| 1. Squelette temps réel | Le lobby fonctionne et survit aux mises en veille | E03, E04, E05 | Terminée |
| 2. Premier mode : quiz QCM | Une partie complète jouable de bout en bout | E06, E07, E08, E09 | Terminée |
| 3. Résilience | Les erreurs et les crashs deviennent invisibles | E10, E11, E12 | Terminée |
| 4. Buzzer et blind test | Un départage juste et un son maîtrisé | E13, E14, E15 | À faire |
| 5. Questions ouvertes et contenu | Réponses libres et outils de création de packs | E16, E17 | À faire |
| 6. Une vraie soirée | Enchaînement de manches, contrôle total du GM, habillage | E18, E19, E20 | À faire |
| 7. Raspberry Pi et terrain | Une soirée réelle sans PC | E21, E22 | À faire |

---

## Phase 0 — Socle technique

**Objectif :** disposer d'un dépôt outillé et vérifier qu'un téléphone du réseau local atteint le serveur.

### E01 — Dépôt et outillage
- Solution .NET avec les projets `Server`, `Engine`, `Content`, `Contracts` et leurs projets de test. `Directory.Build.props` et `.editorconfig` conformes aux conventions.
- Client Vite + Svelte 5 multi-entrées (`player`, `display`, `gm`), avec ESLint, Prettier et les scripts `check`, `test` et `e2e`.
- Build du front vers `wwwroot`, et proxy de développement vers le serveur .NET.
- Génération des types TypeScript depuis `PartyGame.Contracts`.
- Logs Serilog vers la console et un fichier tournant.
- ADR 0001 : architecture générale (boucle unique, moteur pur, snapshots par rôle).

### E02 — Accès des appareils
- Écoute sur `0.0.0.0` et détection de l'IPv4 privée, avec un choix par configuration si plusieurs interfaces réseau sont actives (choix depuis l'interface GM en E04).
- QR code affiché sur l'écran TV.
- Routage des trois interfaces : `/` pour les joueurs, `/display` pour l'écran TV, `/gm` pour le game master.
- Code GM généré et affiché dans la console au démarrage.

**Critère de sortie :** un téléphone scanne le QR code affiché sur la TV et ouvre la page joueur. `dotnet test` et `npm run check` passent.

**À trancher :** confirmation de Svelte 5 face à Blazor WebAssembly, choix de l'outil de génération des types TypeScript.

---

## Phase 1 — Squelette temps réel

**Objectif :** valider la chaîne complète de bout en bout : intention, boucle de jeu, snapshot, affichage. On valide aussi la reconnexion, qui est le risque principal sur mobile.

### E03 — Boucle de jeu et diffusion
- Hub SignalR fortement typé, avec les rôles et les groupes.
- `GameLoop` alimentée par un `Channel<GameInput>`, un `GameState` immuable, des `Transition` et des effets.
- Snapshots versionnés projetés par rôle, et diffusion après chaque changement d'état.
- Timers qui déposent des entrées `TimerElapsed` dans la file.

### E04 — Lobby
- Un joueur rejoint avec un pseudo (longueur limitée, doublons refusés) et reçoit un jeton du serveur.
- L'écran TV affiche le QR code et la liste des joueurs connectés.
- Le GM voit la liste, peut renommer un joueur (pas d'exclusion), et lance la partie.
- Les inscriptions restent ouvertes après le lancement.
- Le GM choisit l'interface réseau encodée dans le QR code quand plusieurs sont actives (en E02, seule la configuration le permet).

### E05 — Connexion robuste
- Reconnexion automatique par jeton, suivie de l'envoi d'un snapshot frais.
- Indicateur discret après 3 s de coupure, et interactions désactivées tant que l'état n'est pas resynchronisé.
- Synchronisation d'horloge façon NTP, refaite à chaque reconnexion.
- Rechargement automatique du client si l'identifiant de build du serveur diffère du sien.

**Critère de sortie :** trois téléphones, l'écran TV et le GM sont réunis dans le lobby. Un téléphone mis en veille une minute revient dans le lobby sans aucune action de son utilisateur.

---

## Phase 2 — Premier mode : quiz QCM

**Objectif :** une partie complète jouable, qui valide le modèle des modes de jeu et le format des packs.

### E06 — Packs de contenu (v1)
- Format `pack.json` et `schemas/pack.schema.json`, associé dans VS Code pour l'autocomplétion.
- Chargement et validation complète au démarrage : structure, existence des médias, cohérence des données.
- Erreurs de pack présentées au GM avant le lancement, de façon claire et localisée (fichier, chemin, problème).
- Sélection du pack parmi ceux du dossier `packs/`.

### E07 — Modèle des modes de jeu
- Interface `IGameMode`, enregistrement explicite et enchaînement simple des manches.
- Structure `client/src/modes/<mode>/` avec les trois vues de chaque mode.
- Helper de test de non-fuite, réutilisable par tous les modes.

### E08 — Mode quiz QCM
- Phases : présentation de la question, réponses ouvertes avec compte à rebours, verrouillage, révélation.
- Vue joueur : propositions distinguées par la couleur et la forme, avec un choix affiché « en attente » jusqu'à confirmation.
- Vue TV : question, compte à rebours, nombre de réponses reçues, révélation avec la répartition des choix.
- Vue GM : aperçu de la bonne réponse, avancer, passer la question.
- Intentions idempotentes (`ClientSeq`) et file d'envoi côté client.

### E09 — Scores
- Calcul des points par le moteur, selon le barème du mode.
- Classement intermédiaire entre les manches et classement final.

**Critère de sortie :** une partie de 10 questions jouée de bout en bout par trois joueurs. Les tests de non-fuite passent pour chaque phase et chaque rôle.

**À trancher :** barème (points fixes ou bonus de rapidité), temps de réponse global ou défini par question, affichage ou non du choix de chaque joueur à la révélation. Tranché : voir les README des épopées E06 à E09 et l'[ADR 0004](adr/0004-format-et-modele-des-packs.md).

---

## Phase 3 — Résilience

**Objectif :** tenir l'engagement central du projet. Une erreur reste invisible pour les joueurs, et seul le GM est informé quand il peut agir.

### E10 — Erreurs invisibles
- Protection de `GameLoop` : retour à l'état précédent, log, incident GM, puis proposition de passer l'étape si l'erreur se répète.
- Côté client : `<svelte:boundary>` sur chaque vue de mode, handlers globaux, remontée des erreurs via `ReportClientError`.
- Écran TV jamais vide, et panneau d'incidents discret dans l'interface GM.

### E11 — Reprise après crash
- Persistance atomique de l'état après chaque transition.
- Au redémarrage, le GM se voit proposer de reprendre la partie, et les joueurs se reconnectent d'eux-mêmes.

### E12 — Outillage de test et diagnostic
- Simulateur de joueurs : des bots clients SignalR, paramétrables en nombre et en comportement.
- Tests E2E Playwright multi-contextes (joueurs, TV, GM).
- Tests de chaos : coupure en pleine manche, exception injectée dans un mode, redémarrage du serveur.
- Page de diagnostic réseau, à ouvrir sur place pour détecter une isolation des clients sur le Wi-Fi.

**Critère de sortie :** si on tue le serveur en pleine question, la partie reprend sans que les joueurs fassent quoi que ce soit. Si on injecte une exception, seul le GM en est informé.

**Décisions :** reprise confirmée par le GM, compte à rebours qui reprend le temps restant, nouveau code GM après un redémarrage, manche défaillante passée en entier, bots en .NET, diagnostic réseau sur une page dédiée et dans la console GM. Voir les README des épopées E10 à E12.

---

## Phase 4 — Buzzer et blind test

**Objectif :** un départage juste et un son maîtrisé sur l'écran TV.

### E13 — Buzzer
- Composant buzzer réutilisable : capture sur `pointerdown`, horodatage converti en heure serveur.
- Arbitrage avec fenêtre configurable, et annonce du gagnant sur la TV et sur les téléphones.
- Le GM valide ou refuse la réponse, puis rouvre le buzzer aux autres joueurs.
- Page de diagnostic d'horloge : écart et RTT estimés pour chaque téléphone, et flash synchronisé sur tous les téléphones pour une vérification à l'œil.

### E14 — Audio sur l'écran TV
- Déverrouillage de l'audio : bouton « Démarrer » sur PC, flag de lecture automatique en mode kiosque.
- Préchargement, requêtes partielles (Range), lecture déclenchée à un instant précis en heure serveur.
- Extraits définis par un point de départ et une durée dans le descripteur.
- Échec de lecture d'un média : incident GM, sans aucun impact visible à l'écran.

### E15 — Mode blind test
- Phases : écoute, buzz, réponse validée par le GM, révélation (titre, artiste, visuel facultatif).
- Pause de la musique au buzz, puis reprise après un mauvais buzz.

**Critère de sortie :** un blind test de 10 extraits joué dans une vraie pièce, dont le départage est jugé juste par les joueurs. La dispersion d'horloge mesurée est documentée.

**À trancher :** réponse orale ou saisie sur le téléphone, points séparés pour le titre et l'artiste, pénalité ou blocage temporaire après un mauvais buzz.

---

## Phase 5 — Questions ouvertes et contenu

**Objectif :** gérer les réponses libres et rendre confortable la création de packs.

### E16 — Mode question ouverte
- Saisie de texte sur le téléphone, avec une longueur maximale et un clavier adapté.
- Pré-classement des réponses par le serveur : normalisation (casse, accents, espaces), distance de Levenshtein et variantes acceptées définies dans le descripteur.
- Validation en lot par le GM, avec les suggestions du pré-classement.
- Révélation sur la TV des bonnes réponses et des réponses notables.

### E17 — Outils de création de contenu
- Commande en ligne de commande pour valider un pack sans lancer de partie.
- Packs au format zip, en plus des dossiers.
- Mode aperçu, pour faire défiler chaque question d'un pack sur l'écran TV.
- Guide de rédaction des packs dans `docs/`, et un pack d'exemple couvrant tous les modes.
- Documentation de chaque mode dans `docs/modes/<mode>.md`.

**Critère de sortie :** un pack mêlant QCM, blind test et questions ouvertes peut être écrit sans lire le code, validé en ligne de commande, puis joué.

---

## Phase 6 — Une vraie soirée

**Objectif :** passer d'une succession de manches à une soirée animée et maîtrisée par le GM.

### E18 — Enchaînement des manches
- Pack multi-manches de modes différents, avec un écran d'introduction pour chaque manche.
- Transitions, classements intermédiaires et podium final.

### E19 — Contrôles avancés du GM
- Pause et reprise de la partie.
- Nouvelle partie sans redémarrer le serveur.
- Ajustement manuel des scores et gestion des arrivées en cours de partie.
- Saut de manche et réordonnancement en direct.

### E20 — Habillage
- Thème visuel et animations de l'écran TV.
- Jingles et sons d'ambiance, sous forme de fichiers locaux joués par l'écran TV.
- Vérification de l'accessibilité : contraste, informations jamais portées par la couleur seule.

**Critère de sortie :** une soirée test d'une heure, avec au moins trois modes, menée par le GM sans toucher au PC hôte.

**À trancher :** jeu en équipes ou en individuel, et participation à la manche en cours et points de départ d'un joueur qui arrive en cours de partie (les inscriptions restent ouvertes, décision E04).

---

## Phase 7 — Raspberry Pi et terrain

**Objectif :** une soirée réelle hébergée sur un Raspberry Pi, dans un lieu inconnu.

### E21 — Déploiement sur Raspberry Pi
- Publication autonome `linux-arm64`, service systemd et démarrage automatique.
- Chromium en mode kiosque sur l'écran TV, avec la lecture automatique autorisée.
- Test de charge avec 20 bots sur le Pi : latence de diffusion et fluidité de l'affichage TV.

### E22 — Préparation du terrain
- Procédure de repli réseau : partage de connexion, routeur de voyage.
- Guide du GM et checklist d'avant soirée : diagnostic réseau, test audio, choix du pack.
- Répétition générale dans des conditions réalistes.

**Critère de sortie :** une soirée complète menée avec le Pi comme seul hôte.

---

## Idées non planifiées

À reprendre lors des points d'étape pour éventuellement créer de nouvelles épopées.

- **Nouveaux modes :**
  - estimation numérique, où la réponse la plus proche gagne ;
  - vrai ou faux éclair ;
  - remise dans l'ordre chronologique ;
  - vote du groupe (« qui est le plus susceptible de… ») ;
  - image révélée progressivement ;
  - devinettes en émojis ;
  - mise de points avant la question ;
  - élimination jusqu'au dernier survivant.
- **HTTPS en local** (domaine réel pointant vers l'IP locale, certificat via validation DNS), qui débloquerait la caméra, le micro et les capteurs, et donc des modes photo, chant ou « secouer le téléphone ».
- **Hébergement en ligne.**
- **Historique et statistiques des soirées**, et export des scores.

---

## Modèle de user story

### Identifiants et statuts

- Épopées : `E01` à `E22`, puis numérotation continue pour les suivantes.
- User stories : `US-<épopée>-<numéro>`, par exemple `US-E08-03`.
- Statuts : À faire, Prête, En cours, Terminée.

### Acteurs

- **Joueur :** interagit avec son téléphone.
- **Public :** ce que tout le monde voit et entend sur l'écran TV.
- **Game master :** pilote la partie depuis son interface.
- **Auteur de pack :** prépare le contenu en amont.
- **Opérateur :** installe et lance le serveur, en pratique souvent le GM.

Les US purement techniques des phases 0 et 1 omettent la ligne « En tant que » et décrivent directement le résultat attendu et sa vérification.

### Gabarit

```markdown
### US-E00-00 — Titre court

**En tant que** <acteur>
**je veux** <comportement>
**afin de** <bénéfice>

**Critères d'acceptation**
- Étant donné <contexte>, quand <action>, alors <résultat observable>
- …

**Comportement en cas d'erreur**
Ce que voit chaque rôle (joueur, public, GM) si l'action échoue, si la connexion est perdue ou si le contenu est invalide.
Par défaut : rien côté joueurs et public, un incident actionnable côté GM.

**Notes techniques**
Intentions, projections et effets concernés, ADR liés, dépendances vers d'autres US.

**Hors périmètre**
Ce qui est volontairement exclu de cette US.
```

Les US se rangent dans un dossier "UserStories" à la racine

### Définition de prêt

Une US est prête quand ses critères d'acceptation et son comportement en cas d'erreur sont rédigés, ses dépendances terminées, et les points « à trancher » qui la concernent décidés.

### Définition de terminé

Une US est terminée quand la checklist « Avant de considérer une tâche terminée » de `docs/coding-guidelines.md` est respectée et que ses critères d'acceptation ont été vérifiés sur de vrais appareils si elle touche une interface.
