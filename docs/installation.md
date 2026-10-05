# Installation et lancement

Ce guide s'adresse à l'opérateur : la personne qui installe et lance le serveur le soir de la partie, en pratique souvent le GM. Il sera complété pour le Raspberry Pi (E21) et la préparation sur le terrain (E22).

## Lancer le serveur

Depuis la racine du dépôt, dans PowerShell :

```powershell
.\scripts\start.ps1
```

Le script vérifie que tout est en ordre avant de lancer le serveur, et s'arrête avec un message précis sinon :

- le SDK .NET demandé par `global.json` et Node.js (20.19 au minimum) sont installés ;
- les dépendances du front sont installées (`npm ci`) si elles manquent ou si `package-lock.json` a changé ;
- le front est reconstruit (`npm run build`) si ses sources ou `PartyGame.Contracts` ont changé depuis le dernier build, après avoir vérifié que les types TypeScript générés sont à jour. Une empreinte des sources est conservée dans `wwwroot/.build-stamp` : un build lancé par un autre moyen est donc refait au lancement suivant ;
- le port est libre, et aucun réseau n'est déclaré « public » dans Windows (voir le pare-feu plus bas).

Paramètres : `-Port 5001` pour changer de port, `-GameMasterCode 123456` pour imposer le code GM (développement seulement), `-Rebuild` pour forcer la reconstruction du front. `Get-Help .\scripts\start.ps1 -Detailed` les décrit. Le script charge les packs du dossier `packs/` du dépôt (voir « Dossier des packs »).

Sans le script, l'équivalent manuel est :

```bash
cd client && npm install && npm run build && cd ..
dotnet run --project src/PartyGame.Server
```

Le serveur .NET sert le dernier build du front : après une modification du front, sans le script, relancer `npm run build`.

Le serveur écoute en HTTP sur le port 5000 de **toutes** les interfaces IPv4 (`0.0.0.0`), et pas seulement sur `localhost`. La console affiche `Now listening on: http://0.0.0.0:5000`. Sur le PC hôte, la page joueur reste accessible à l'adresse `http://localhost:5000/`.

Une fois prêt, le serveur affiche dans la console une bannière qui donne tout ce qu'il faut pour lancer la soirée :

```
==============================================================
  PartyGame est prêt

  Adresse des joueurs : 192.168.1.42 (détectée)
  Écran TV            : http://192.168.1.42:5000/display/
  Game master         : http://192.168.1.42:5000/gm/
  Code game master    : 482913

  Packs (C:\PartyGame\packs) :
    quiz-exemple        : « Quiz d'exemple », 2 manches, valide
==============================================================
```

Ouvrir l'URL « Écran TV » sur le navigateur de la TV, et l'URL « Game master » sur l'appareil du GM.

L'écran TV affiche un QR code et, en clair, l'adresse de la page joueur (`http://192.168.1.42:5000/` dans l'exemple). Les joueurs le scannent avec l'appareil photo de leur téléphone, ou tapent l'adresse dans leur navigateur. Le QR code reprend le port de la page ouverte sur la TV : en développement, un écran TV ouvert sur le port 5173 de Vite envoie les téléphones vers Vite. Tant que le serveur ne connaît aucune adresse, l'écran affiche un message d'attente et réessaie toutes les 5 secondes.

Une fois la partie lancée, les inscriptions restent ouvertes : pour un retardataire, le bouton « Afficher le QR code sur la TV » de la console du game master l'affiche en haut à gauche de l'écran TV, par-dessus le jeu, jusqu'à ce que le GM le masque.

## Code game master

N'importe qui sur le réseau peut ouvrir `/gm/` : seul le code game master permet de piloter la partie. C'est un code de 6 chiffres, tiré au hasard à chaque démarrage du serveur et affiché uniquement dans la bannière de la console. Il n'est jamais écrit dans les fichiers de `logs/` ni envoyé par le serveur à un navigateur. Après un redémarrage, y compris une reprise après crash, relire le nouveau code dans la console.

Pour le développement et les tests E2E, un code fixe peut être imposé par le paramètre `GameMaster:Code` (la bannière l'indique alors par « imposé par GameMaster:Code ») :

| Moyen | Exemple |
|---|---|
| Fichier `appsettings.json` du serveur | `"GameMaster": { "Code": "123456" }` |
| Variable d'environnement | `GameMaster__Code=123456` (PowerShell : `$env:GameMaster__Code = "123456"`) |
| Argument de ligne de commande | `dotnet run --project src/PartyGame.Server -- --GameMaster:Code=123456` |

Une valeur qui n'est pas faite d'exactement 6 chiffres arrête le serveur au démarrage. Un code imposé reste lisible par quiconque accède au fichier ou à l'environnement du PC : ne pas l'utiliser pour une vraie soirée.

## Adresse annoncée aux téléphones

L'adresse des joueurs est celle que l'écran TV encode dans son QR code. Le serveur la détermine seul au démarrage :

- il ne retient que les adresses IPv4 privées (`10.x.x.x`, `172.16.x.x` à `172.31.x.x`, `192.168.x.x`) des interfaces actives ;
- il écarte les interfaces virtuelles (Hyper-V `vEthernet`, WSL, Docker, VirtualBox, VMware) et les VPN (WireGuard, Tailscale, ZeroTier, OpenVPN…), ainsi que les adresses en lien local (`169.254.x.x`) ou en CGNAT (`100.64.x.x` à `100.127.x.x`) ;
- s'il reste plusieurs candidates, il préfère celle qui a une passerelle par défaut (la box ou le routeur), puis la plus petite adresse : le choix ne change pas d'un démarrage à l'autre tant que le réseau ne change pas.

Si le PC est relié à plusieurs réseaux (Ethernet et Wi-Fi par exemple), la bannière liste les autres adresses possibles. Le plus simple est alors de choisir la bonne depuis la console du game master (« Adresse des joueurs ») : le QR code de l'écran TV change aussitôt, sans redémarrage. Ce choix est oublié au redémarrage du serveur. Pour imposer une adresse de façon durable, relancer le serveur avec le paramètre `Network:AdvertisedAddress` :

| Moyen | Exemple |
|---|---|
| Fichier `appsettings.json` du serveur | `"Network": { "AdvertisedAddress": "192.168.1.42" }` |
| Variable d'environnement | `Network__AdvertisedAddress=192.168.1.42` (PowerShell : `$env:Network__AdvertisedAddress = "192.168.1.42"`) |
| Argument de ligne de commande | `dotnet run --project src/PartyGame.Server -- --Network:AdvertisedAddress=192.168.1.42` |

L'adresse imposée est retenue telle quelle. Si aucune interface active ne la porte, le serveur démarre quand même mais l'indique dans la bannière et dans les logs. Une valeur qui n'est pas une adresse IPv4 (`192.168.1.42`) arrête le serveur au démarrage.

L'adresse est calculée une seule fois : après un changement de réseau, relancer le serveur. Si le PC n'est connecté à aucun réseau, le serveur démarre, la bannière invite à connecter le PC au Wi-Fi, et l'écran TV affiche un message d'attente à la place du QR code.

## Dossier des packs

Au démarrage, avant d'accepter la moindre connexion, le serveur charge et vérifie entièrement chaque pack du dossier des packs : chaque sous-dossier qui contient un fichier `pack.json` (nom exact, en minuscules) est un pack, et son identifiant est le nom du sous-dossier. Les autres sous-dossiers sont ignorés. Le descripteur n'est relu que sur demande : après avoir corrigé un pack, le GM appuie sur « Actualiser les packs » dans sa console, et le serveur recharge et revérifie tous les packs. Une fois la partie lancée, le pack choisi est copié dans la partie : modifier ses fichiers n'a plus d'effet, et l'actualisation n'est plus proposée.

La bannière liste les packs, avec pour chacun son titre, son nombre de manches et son état : valide, ou invalide avec le nombre de problèmes. Chaque problème est détaillé dans le journal, en `WRN`, avec le fichier, le chemin dans le descripteur (par exemple `$.rounds[1].questions[4].choices`) et ses paramètres. Un pack invalide n'empêche jamais le démarrage : il ne pourra simplement pas être choisi. Un dossier des packs absent ou vide non plus : la bannière le signale.

Dans le lobby, la console GM liste les mêmes packs, avec leurs manches et leur mode de jeu. Le GM y choisit le pack à jouer, dont l'écran TV affiche le titre ; s'il n'y a qu'un pack valide, il est choisi d'office. Pour un pack invalide, « Voir les problèmes » détaille chaque problème en français, avec le fichier et le chemin dans le descripteur. Tant qu'aucun pack n'est choisi, la partie ne peut pas être lancée.

Le dossier se règle par le paramètre `Packs:Directory`. Par défaut, c'est le dossier `packs` à côté de l'exécutable du serveur. Un chemin relatif est lui aussi relatif au dossier de l'exécutable, et non au dossier courant. `scripts/start.ps1` indique le dossier `packs/` du dépôt, sauf si la variable d'environnement `Packs__Directory` en désigne un autre.

| Moyen | Exemple |
|---|---|
| Fichier `appsettings.json` du serveur | `"Packs": { "Directory": "D:\\Soirees\\packs" }` |
| Variable d'environnement | `Packs__Directory=D:\Soirees\packs` (PowerShell : `$env:Packs__Directory = "D:\Soirees\packs"`) |
| Argument de ligne de commande | `dotnet run --project src/PartyGame.Server -- --Packs:Directory=D:\Soirees\packs` |

Les médias référencés par un pack doivent se trouver dans son dossier, avec exactement la même casse que dans `pack.json` : Windows ne la distingue pas, mais le Raspberry Pi si, et le serveur la vérifie partout pour qu'un pack préparé sur un PC fonctionne aussi sur le Pi.

## Enregistrement de la partie

Après chaque changement, le serveur enregistre la partie dans le fichier `current-game.json` du dossier de données, pour pouvoir la reprendre après un crash ou un redémarrage. L'écriture passe par un fichier temporaire (`current-game.json.tmp`) puis remplace l'ancien fichier : un arrêt brutal laisse toujours le dernier enregistrement complet. Ce fichier contient les jetons des joueurs et les bonnes réponses : il n'est servi par aucune page, mais ne doit pas être partagé.

Le dossier se règle par le paramètre `Persistence:Directory` (variable d'environnement `Persistence__Directory`), comme celui des packs. Par défaut, c'est le dossier `data` à côté de l'exécutable du serveur ; un chemin relatif est lui aussi relatif au dossier de l'exécutable. Le serveur le crée au besoin. S'il ne peut ni le créer ni y écrire, il s'arrête au démarrage avec un message `FTL` qui nomme le dossier. Si l'écriture échoue en cours de partie (disque plein, par exemple), la partie continue et la console GM affiche un incident jusqu'à ce que l'enregistrement fonctionne de nouveau.

### Reprise après un crash ou un redémarrage

Au démarrage, si `current-game.json` contient une partie avec au moins un joueur, le serveur ne la reprend pas d'office : la bannière de la console l'annonce, et la console GM, une fois le **nouveau** code saisi, décrit la partie trouvée (pack, avancement, nombre de joueurs, heure de l'enregistrement) avec deux choix :

- **Reprendre la partie** : les téléphones, l'écran TV et la console la retrouvent là où elle en était, sans que les joueurs aient rien à faire. Un compte à rebours en cours repart avec le temps qui lui restait. Si des médias du pack ont disparu du disque depuis, le bouton reste désactivé et la console les liste : remettez-les à leur place, puis « Vérifier de nouveau ».
- **Nouvelle partie** : après confirmation, la partie trouvée est mise de côté dans `previous-game.json` (qui remplace le précédent), les packs sont relus et une partie neuve démarre dans le lobby. Les joueurs se réinscrivent, leur pseudo prérempli.

En attendant la décision, les téléphones affichent « Retour dans la partie… » (ou un écran d'attente s'ils ne s'étaient jamais inscrits) et l'écran TV « Reprise de la partie… ». Une partie sans joueur n'est pas proposée.

Un fichier illisible, tronqué ou enregistré par une version du serveur au format différent est renommé `current-game.unreadable-<date>-<heure>.json`, une nouvelle partie démarre, et la console GM affiche un incident : les joueurs doivent se réinscrire. Pour repartir de zéro sans passer par la console, il suffit de supprimer `current-game.json` serveur arrêté.

| Fichier du dossier de données | Rôle |
|---|---|
| `current-game.json` | La partie en cours, réécrite après chaque changement |
| `current-game.json.tmp` | Écriture en cours ; un fichier abandonné par un crash est supprimé au démarrage suivant |
| `previous-game.json` | La dernière partie trouvée au démarrage que le GM a choisi de ne pas reprendre |
| `current-game.unreadable-*.json` | Un enregistrement que le serveur n'a pas pu relire |

## Changer de port

Le port se règle par le paramètre `Network:Port` (5000 par défaut), sans recompiler. Par ordre de priorité croissante :

| Moyen | Exemple |
|---|---|
| Fichier `appsettings.json` du serveur | `"Network": { "Port": 5001 }` |
| Variable d'environnement | `Network__Port=5001` (PowerShell : `$env:Network__Port = "5001"`) |
| Argument de ligne de commande | `dotnet run --project src/PartyGame.Server -- --Network:Port=5001` |

Si le port est déjà occupé (un autre serveur PartyGame encore lancé, une autre application), le serveur s'arrête immédiatement avec un message `FTL` qui nomme le port et rappelle comment en choisir un autre.

En développement, le serveur Vite relaie `/api`, `/hub` et `/media` vers `http://localhost:5000`. Après un changement de port, définir aussi `PARTYGAME_SERVER_URL` (par exemple `http://localhost:5001`) avant `npm run dev`.

## Autoriser le port dans le pare-feu Windows

Sans règle de pare-feu, le serveur fonctionne sur le PC hôte mais les téléphones n'atteignent pas la page.

1. **Profil réseau.** Le Wi-Fi de la soirée doit être déclaré comme réseau **privé** : Paramètres > Réseau et Internet > Wi-Fi > *nom du réseau* > Type de profil réseau > Privé. Sur un réseau public, Windows bloque les connexions entrantes.
2. **Règle de pare-feu.** Deux possibilités :
   - au premier lancement, Windows peut afficher une invite « Le Pare-feu Windows Defender a bloqué certaines fonctionnalités ». Cocher **Réseaux privés** puis cliquer sur **Autoriser l'accès**. L'invite vise l'exécutable lancé (`dotnet.exe` ou `PartyGame.Server.exe`) ;
   - sinon, ouvrir un terminal **en tant qu'administrateur** et ajouter une règle pour le port :

     ```powershell
     netsh advfirewall firewall add rule name="PartyGame" dir=in action=allow protocol=TCP localport=5000 profile=private
     ```

     Pour la retirer : `netsh advfirewall firewall delete rule name="PartyGame"`. Adapter `localport` si le port a été changé.

Le serveur n'ajoute jamais de règle lui-même : cela exigerait des droits administrateur.

## Vérifier qu'un téléphone atteint le serveur

1. Lire l'adresse des joueurs dans la bannière de démarrage (par exemple `192.168.1.42`). À défaut, `ipconfig` dans un terminal donne la ligne « Adresse IPv4 » de la carte Wi-Fi ou Ethernet.
2. Sur le PC, ouvrir `http://localhost:5000/health` : la page doit afficher `Healthy`.
3. Sur un téléphone connecté **au même Wi-Fi**, ouvrir `http://192.168.1.42:5000/health`, puis `http://192.168.1.42:5000/` pour la page joueur.

Si le PC répond mais pas le téléphone :

- vérifier que le téléphone n'utilise pas ses données mobiles ou un autre Wi-Fi (réseau invité, répéteur isolé) ;
- vérifier le profil réseau et la règle de pare-feu ci-dessus ;
- certains Wi-Fi publics ou d'hôtel isolent les appareils entre eux : aucune configuration du PC n'y remédie. Utiliser alors un partage de connexion ou un routeur de voyage (voir E22).

## Diagnostic sur place

En arrivant dans un lieu inconnu, avant l'arrivée des invités, le GM vérifie en une minute que le Wi-Fi convient.

1. Ouvrir la console du game master : la section « Diagnostic réseau » donne l'adresse de la page de diagnostic (`http://192.168.1.42:5000/diagnostic/` par exemple) et son QR code. L'écran TV ne l'affiche pas.
2. Scanner ce QR code avec un ou deux téléphones, si possible un iPhone et un Android. Le test dure une vingtaine de secondes et n'inscrit pas le téléphone : il n'apparaît ni dans le lobby ni dans la liste des joueurs.
3. La page donne un verdict (« Tout est bon », « Utilisable, avec des réserves » ou « Problème »), un conseil pour chaque mesure en défaut, et un bouton « Relancer le test ». La console du game master liste les derniers diagnostics : type d'appareil, heure, verdict et temps de réponse médian.

Le test mesure la connexion au serveur et son mode (WebSocket, ou un repli plus lent), le temps de réponse sur une salve (médiane, maximum, gigue), la stabilité sur 15 secondes (un aller-retour par seconde, pertes et coupures), le débit d'un téléchargement d'environ 1 Mo, et si le téléphone est sur le même réseau que l'adresse annoncée. Seuils : un temps de réponse médian sous 50 ms est bon, jusqu'à 150 ms il est utilisable avec des réserves, au-delà c'est un problème. Une perte de plus de 2 %, une coupure, un mode de repli, un autre réseau ou un débit sous 2 Mbit/s donnent des réserves. La page ne teste pas l'accès à Internet, dont le jeu n'a pas besoin.

Pendant la partie, la liste des joueurs de la console montre aussi, pour chaque téléphone et pour l'écran TV, le temps de réponse récent, l'incertitude de son horloge, le mode de connexion et le nombre de reconnexions, avec un repère ⚠ sur une valeur mauvaise.

### Horloge et flash synchronisé

Le buzzer départage les joueurs d'après l'heure de leur appui, convertie en heure serveur par l'horloge synchronisée du téléphone. La page de diagnostic affiche, dans sa section « Horloge », l'aller-retour de la synchronisation et l'incertitude qui en découle (la moitié de l'aller-retour) : l'heure serveur estimée par le téléphone est juste à cette marge près. La console du game master marque d'un ⚠ un téléphone dont l'incertitude dépasse 50 ms.

Pour vérifier à l'œil, ouvrir la page de diagnostic sur plusieurs téléphones (et sur l'écran TV) posés côte à côte, puis toucher « Flash synchronisé » sur chacun : l'écran devient blanc pendant 100 ms à chaque seconde entière de l'heure serveur. Des horloges bien alignées clignotent ensemble. Toucher l'écran arrête le flash. Le protocole de mesure et les résultats sont dans [mesures/dispersion-horloge.md](mesures/dispersion-horloge.md).

Si un téléphone n'arrive même pas à ouvrir la page de diagnostic :

- vérifier qu'il est connecté **au même Wi-Fi** que le serveur, et pas à ses données mobiles ;
- se méfier d'un **réseau invité** : il est souvent séparé du réseau principal, où se trouve le PC ;
- certains points d'accès pratiquent l'**isolation des clients** (« AP isolation », « client isolation ») : les appareils du Wi-Fi ne se voient pas entre eux. Elle se désactive dans l'administration de la box ou du point d'accès, quand on y a accès ;
- tester depuis le PC hôte lui-même, avec l'adresse locale (`http://localhost:5000/diagnostic/`) : si la page fonctionne sur le PC mais pas sur les téléphones, le problème vient du réseau ou du pare-feu (voir plus haut), pas du serveur ;
- en dernier recours, se replier sur un partage de connexion ou un routeur de voyage (voir E22).
