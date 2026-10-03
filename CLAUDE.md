# PartyGame

Party game multijoueur pour animer des soirées : une succession d'activités (quiz à choix multiples, questions ouvertes, blind test, buzzer…) jouées en local. Chaque joueur utilise le navigateur de son téléphone, une TV affiche l'état du jeu, et un game master (GM) pilote la partie depuis sa propre interface.

## Contraintes fondamentales

Ces règles priment sur tout le reste. Si une demande entre en conflit avec l'une d'elles, le signaler avant d'agir.

1. **100 % local à l'exécution.** Une fois le serveur lancé, aucune requête ne sort du réseau local : pas de CDN, pas de Google Fonts, pas d'API externe, pas de télémétrie. Polices, icônes, bibliothèques et médias sont embarqués dans le build ou dans les packs.
2. **Navigateur uniquement côté joueurs.** Aucune installation. Les joueurs rejoignent en scannant un QR code affiché sur la TV.
3. **HTTP simple sur le réseau local (pour l'instant).** La page n'est pas un « secure context » : Wake Lock, `crypto.randomUUID`, `crypto.subtle`, Clipboard API, service workers, caméra, micro et capteurs sont indisponibles. Ne pas les utiliser, ou seulement derrière une détection de fonctionnalité avec repli.
4. **Cibles mobiles : Safari iOS et Chrome Android récents.** Toute interface joueur doit fonctionner sur les deux.

## Architecture

Un serveur ASP.NET Core sert à la fois le front (fichiers statiques) et un hub SignalR temps réel. Il tourne sur un PC (cible principale) ou un Raspberry Pi connecté au Wi-Fi du lieu.

- **Serveur autoritaire.** Le moteur de jeu détient seul l'état de la partie, calcule les scores et tranche (buzzer, validation des réponses). Les clients n'envoient que des intentions (`SubmitAnswer`, `Buzz`, `NextStep`…) et ne contiennent aucune logique de jeu.
- **Snapshots projetés par rôle.** À chaque changement, le serveur envoie l'état complet, versionné par un numéro croissant, filtré selon le rôle du client : `Player`, `Display` ou `GameMaster`. Pas de diffs. Un client affiche le dernier snapshot reçu et ignore toute version plus ancienne.
- **Aucune fuite d'information.** Seule la projection `GameMaster` contient les bonnes réponses avant la révélation : n'importe qui sur le réseau peut ouvrir l'écran TV ou les outils de développement de son téléphone. Une projection `Player` ne contient jamais les réponses des autres joueurs avant la révélation. Chaque projection est couverte par un test qui le vérifie.
- **Reconnexion transparente.** Un joueur est identifié par un jeton généré par le serveur et stocké dans le `localStorage` de son téléphone. Après une veille ou un rechargement, il rejoint avec ce jeton et reçoit le snapshot courant. Ne jamais lier l'identité d'un joueur à un `ConnectionId` SignalR.
- **Interface GM protégée.** Les intentions GM sont refusées sans le code GM : 6 chiffres régénérés à chaque démarrage, affichés dans la bannière de la console et jamais dans les logs ni dans une réponse HTTP. `GameMaster:Code` l'impose pour le développement et les tests E2E.
- **Modes de jeu modulaires.** Chaque activité est un module : une machine à états côté serveur (implémente `IGameMode`) et trois vues côté client (`player`, `display`, `gm`) dans `client/src/modes/<mode>/`. Ajouter un mode ne doit nécessiter aucune modification du moteur en dehors de son enregistrement. Côté client, ses vues sont déclarées dans `client/src/modes/registry.ts`, dont l'exhaustivité est vérifiée à la compilation, et la règle ESLint `partygame/mode-boundaries` limite les imports d'un mode à `src/shared/` et à son propre dossier.
- **Le son ne sort que de l'écran TV.** Les téléphones ne jouent jamais d'audio.

## Temps et buzzer

- Côté serveur, le temps passe exclusivement par un `TimeProvider` injecté, jamais par `DateTime.Now` ou `DateTime.UtcNow`, afin de tester de façon déterministe avec `FakeTimeProvider`.
- Chaque client synchronise son horloge avec le serveur façon NTP : salve d'allers-retours, conservation des échantillons au RTT minimal, resynchronisation périodique et à chaque reconnexion.
- Un buzz est capturé sur `pointerdown` (jamais `click`), horodaté avec `performance.now()`, puis converti en heure serveur avant envoi.
- Après le premier buzz reçu, le serveur attend une fenêtre d'arbitrage configurable (250 ms par défaut) et désigne le gagnant selon l'horodatage, pas selon l'ordre d'arrivée.
- Les ordres de lecture audio envoyés à l'écran TV portent un instant de déclenchement en heure serveur. L'écran précharge le média avant cet instant.

## Packs de contenu

Un pack est un dossier (les zip arrivent en E17) contenant un descripteur `pack.json` et ses médias (MP3, images). Des exemples se trouvent dans `packs/`.

- Chaque activité du descripteur a un champ `type` qui désigne son mode de jeu (désérialisation polymorphe avec System.Text.Json).
- Le schéma de référence est `schemas/pack.schema.json`. Il est généré depuis les types C# du descripteur (`PartyGame.Contracts.Packs`) par `npm run generate:contracts`, à chaque évolution de leur format. `.vscode/settings.json` l'associe aux fichiers `packs/*/pack.json`.
- Un pack est entièrement validé au chargement : structure, existence des médias référencés, cohérence des données. Une partie ne doit jamais échouer en cours de route à cause du contenu. Les erreurs sont remontées au GM avant le lancement, avec un message précis (fichier, chemin dans le descripteur, problème).
- Les médias sont servis avec prise en charge des requêtes partielles (Range), pour pouvoir démarrer un extrait au milieu d'un morceau. Ils le sont sous `/media/<identifiant>`, un identifiant tiré au hasard au lancement de la partie : le chemin d'un média n'apparaît jamais dans une URL ni dans une projection.

## Stack

| Couche | Choix |
|---|---|
| Serveur | .NET 10, ASP.NET Core, SignalR |
| Front | TypeScript, Vite, Svelte 5 : une application multi-entrées (`player`, `display`, `gm`) |
| Client temps réel | `@microsoft/signalr` avec `withAutomaticReconnect` |
| Tests .NET | xUnit, `FakeTimeProvider` |
| Tests front et E2E | Vitest, Playwright (émulation mobile, un contexte de navigateur par joueur simulé) |

## Arborescence

```
src/
  PartyGame.Server/      Hôte ASP.NET Core : hub SignalR, endpoints, fichiers statiques, QR code, détection de l'IP locale
  PartyGame.Engine/      Moteur de jeu pur (aucune dépendance ASP.NET) : état, modes, scores, arbitrage
  PartyGame.Content/     Chargement et validation des packs
  PartyGame.Contracts/   Messages et DTO échangés avec les clients, source des types TypeScript
client/
  src/player/            Point d'entrée de l'interface joueur
  src/display/           Point d'entrée de l'écran TV
  src/gm/                Point d'entrée de l'interface game master
  src/shared/            Connexion SignalR, synchro d'horloge, store du snapshot, composants communs
  src/shared/contracts/  Types TypeScript générés depuis PartyGame.Contracts (versionnés, jamais modifiés à la main)
  src/modes/<mode>/      Vues player, display et gm de chaque mode de jeu
tools/
  PartyGame.TypeGen/     Générateur des types TypeScript, par réflexion sur PartyGame.Contracts
tests/                   Un projet de test par projet de src/ et de tools/
packs/                   Packs d'exemple, utilisés en développement et dans les tests
schemas/                 JSON Schema des descripteurs de packs
docs/                    Documentation et décisions d'architecture
```

Sens des dépendances : `Contracts` ne dépend de rien, `Engine` et `Content` ne dépendent que de `Contracts`, `Server` dépend de tous. L'outil `TypeGen` ne dépend que de `Contracts`. `Engine` ne référence jamais ASP.NET Core ni SignalR.

## Commandes

Cette section décrit les commandes de référence. La mettre à jour dès qu'un script change.

```bash
# Lancement complet (PowerShell) : prérequis, npm ci, contrats, build du front si besoin, port libre, puis serveur
.\scripts\start.ps1            # -Port 5001, -GameMasterCode 123456, -Rebuild ; charge les packs de packs/
# Test sur un seul PC (serveur déjà lancé) : écran TV, GM et N joueurs en fenêtres privées isolées
.\scripts\open-browsers.ps1 3  # -BaseUrl http://localhost:5173 (Vite), -Port 5001, -Browser chrome|edge

# Serveur
dotnet build
dotnet test
dotnet run --project src/PartyGame.Server   # port 5000 sur 0.0.0.0 ; sert le front construit dans wwwroot
                                            # Network:Port change le port (Network__Port=5001 ou -- --Network:Port=5001)
                                            # Packs:Directory désigne le dossier des packs (défaut : packs à côté de l'exécutable)

# Front (depuis client/)
npm install
npx playwright install chromium webkit   # une fois : navigateurs des tests E2E
npm run dev        # port 5173 sur toutes les interfaces : pages /, /display/, /gm/ ; /api, /hub et /media relayés au serveur .NET
                   # PARTYGAME_SERVER_URL change la cible du proxy (défaut http://localhost:5000)
npm run build      # sortie dans src/PartyGame.Server/wwwroot (non versionné), servie par le serveur .NET
npm run check      # types générés à jour (si dotnet est présent) + svelte-check + tsc + ESLint + Prettier
npm run generate:contracts   # régénère src/shared/contracts et schemas/pack.schema.json depuis PartyGame.Contracts (SDK .NET requis)
npm run format     # reformatage Prettier
npm run test       # Vitest
npm run e2e        # Playwright sur le build : iPhone (WebKit), Pixel (Chromium), desktop
                   # démarre aussi le serveur .NET (port 5199, GameMaster:Code=246810, packs de e2e/packs) derrière le proxy
                   # le projet « launch » (lancement de la partie, changement d'adresse : tout le serveur partagé) passe après tous les autres

# Publication pour Raspberry Pi
dotnet publish src/PartyGame.Server -c Release -r linux-arm64 --self-contained
```

## Points d'attention

- **Écoute réseau.** Kestrel écoute sur `0.0.0.0` et non sur `localhost`, sinon les téléphones ne peuvent pas se connecter. Le port vient de l'option typée `NetworkOptions` (`Network:Port`), qui remplace toute URL passée par `urls` : ne pas ajouter de `launchSettings.json` ni de `Kestrel:Endpoints`. Sous Windows, le pare-feu doit autoriser le port (voir [docs/installation.md](docs/installation.md)).
- **Routage des pages.** `/display` et `/gm` (sans barre oblique finale ou dans une autre casse) sont redirigés temporairement vers `/display/` et `/gm/`, côté serveur comme dans Vite (plugin `client/vite/canonicalPages.ts`). Un navigateur qui ouvre une page inconnue est redirigé vers `/`, sauf sous les préfixes techniques réservés, définis dans `ServerPaths` (`/api`, `/hub`, `/media`, `/assets`, `/health`) : tout nouveau préfixe technique y est déclaré.
- **Hub SignalR.** Un seul hub, `GameHub`, sous `/hub/game`. Ses méthodes reçoivent leur message en `JsonElement` et le lisent avec `HubMessage.TryRead`, pour journaliser en `Warning` un message malformé (que SignalR écarterait sinon sans trace visible). Une intention réservée au GM porte `[GameMasterOnly]`. Les intentions de manche de tous les modes passent par deux méthodes génériques, `SendRoundIntent` et `SendGameMasterRoundIntent` : un mode n'ajoute aucune méthode au hub. Le client ne passe que par `client/src/shared/connection` : une règle ESLint interdit d'importer `@microsoft/signalr` ailleurs.
- **QR code.** Il encode l'IPv4 privée détectée au démarrage, jamais un nom en `.local` (mal résolu sur Android). Si plusieurs interfaces réseau sont actives, le GM choisit la bonne.
- **Autoplay sur l'écran TV.** L'écran affiche un bouton « Démarrer » dont le clic débloque l'audio du navigateur. En mode kiosque Chromium sur le Pi, utiliser le flag `--autoplay-policy=no-user-gesture-required`.
- **Interface joueur.** Appliquer `touch-action: manipulation` sur les zones interactives pour éviter le délai et le zoom au double tap, et rendre le viewport non zoomable.
- **Mise en veille.** À chaque reconnexion SignalR, le client se réidentifie avec son jeton, resynchronise son horloge et attend un snapshot frais avant de réafficher quoi que ce soit d'interactif.

## Façon de travailler

- Toute modification du protocole (messages, DTO, projections) commence par `PartyGame.Contracts`, puis la régénération des types TypeScript (`npm run generate:contracts`), puis l'adaptation des tests.
- Une tâche n'est terminée que si `dotnet test` et `npm run check` passent.
- Ne pas ajouter de dépendance NuGet ou npm sans le signaler et le justifier. Vérifier qu'elle ne fait aucun appel réseau à l'exécution.
- Le moteur se teste sans réseau, en tests unitaires avec `FakeTimeProvider`. Un simulateur de joueurs (bots clients SignalR) sert aux tests de charge et aux tests E2E.
- Pour toute décision marquée « provisoire » ci-dessous, ou absente de ce fichier, proposer les options plutôt que trancher seul.

## Décisions

| Sujet | Statut |
|---|---|
| Serveur .NET + SignalR, état autoritaire, snapshots par rôle | Retenu ([ADR 0001](docs/adr/0001-architecture-generale.md)) |
| Hôte sur PC, Raspberry Pi en cible secondaire | Retenu ([ADR 0001](docs/adr/0001-architecture-generale.md)) |
| Front en TypeScript + Svelte 5 | Retenu ([ADR 0002](docs/adr/0002-front-svelte-5.md)) |
| Génération des types TypeScript par un outil maison | Retenu ([ADR 0003](docs/adr/0003-generation-types-typescript.md)) |
| Packs : descripteurs en JSON, types dans `Contracts.Packs`, schéma généré | Retenu ([ADR 0004](docs/adr/0004-format-et-modele-des-packs.md)) |
| HTTPS en local | Reporté (piste : domaine réel pointant vers l'IP locale + certificat Let's Encrypt via validation DNS) |
| Hébergement en ligne | Hors périmètre pour l'instant |

## Vocabulaire

| Domaine | Nom dans le code |
|---|---|
| Partie | `Game` |
| Pack de contenu | `Pack` |
| Mode de jeu (quiz, blind test…) | `GameMode` |
| Manche : une activité du pack jouée pendant la partie | `Round` |
| Message envoyé par un client | `Intent` |
| État complet filtré pour un rôle | `Snapshot` |
| Rôle d'un client | `Role` (`Player`, `Display`, `GameMaster`) |

## Conventions de code

Les conventions détaillées sont dans @docs/coding-guidelines.md

## Roadmap

La roadmap se trouve dans docs/roadmap.md