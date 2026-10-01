# Installation et lancement

Ce guide s'adresse à l'opérateur : la personne qui installe et lance le serveur le soir de la partie, en pratique souvent le GM. Il sera complété pour le Raspberry Pi (E21) et la préparation sur le terrain (E22).

## Lancer le serveur

Depuis la racine du dépôt :

```bash
cd client && npm install && npm run build && cd ..
dotnet run --project src/PartyGame.Server
```

Le serveur écoute en HTTP sur le port 5000 de **toutes** les interfaces IPv4 (`0.0.0.0`), et pas seulement sur `localhost`. La console affiche `Now listening on: http://0.0.0.0:5000`. Sur le PC hôte, la page joueur reste accessible à l'adresse `http://localhost:5000/`.

Une fois prêt, le serveur affiche dans la console une bannière qui donne tout ce qu'il faut pour lancer la soirée :

```
==============================================================
  PartyGame est prêt

  Adresse des joueurs : 192.168.1.42 (détectée)
  Écran TV            : http://192.168.1.42:5000/display/
  Game master         : http://192.168.1.42:5000/gm/
  Code game master    : 482913
==============================================================
```

Ouvrir l'URL « Écran TV » sur le navigateur de la TV, et l'URL « Game master » sur l'appareil du GM.

L'écran TV affiche un QR code et, en clair, l'adresse de la page joueur (`http://192.168.1.42:5000/` dans l'exemple). Les joueurs le scannent avec l'appareil photo de leur téléphone, ou tapent l'adresse dans leur navigateur. Le QR code reprend le port de la page ouverte sur la TV : en développement, un écran TV ouvert sur le port 5173 de Vite envoie les téléphones vers Vite. Tant que le serveur ne connaît aucune adresse, l'écran affiche un message d'attente et réessaie toutes les 5 secondes.

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

Si le PC est relié à plusieurs réseaux (Ethernet et Wi-Fi par exemple), la bannière liste les autres adresses possibles. Pour en imposer une, relancer le serveur avec le paramètre `Network:AdvertisedAddress` :

| Moyen | Exemple |
|---|---|
| Fichier `appsettings.json` du serveur | `"Network": { "AdvertisedAddress": "192.168.1.42" }` |
| Variable d'environnement | `Network__AdvertisedAddress=192.168.1.42` (PowerShell : `$env:Network__AdvertisedAddress = "192.168.1.42"`) |
| Argument de ligne de commande | `dotnet run --project src/PartyGame.Server -- --Network:AdvertisedAddress=192.168.1.42` |

L'adresse imposée est retenue telle quelle. Si aucune interface active ne la porte, le serveur démarre quand même mais l'indique dans la bannière et dans les logs. Une valeur qui n'est pas une adresse IPv4 (`192.168.1.42`) arrête le serveur au démarrage.

L'adresse est calculée une seule fois : après un changement de réseau, relancer le serveur. Si le PC n'est connecté à aucun réseau, le serveur démarre, la bannière invite à connecter le PC au Wi-Fi, et l'écran TV affiche un message d'attente à la place du QR code.

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
