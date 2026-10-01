# Installation et lancement

Ce guide s'adresse à l'opérateur : la personne qui installe et lance le serveur le soir de la partie, en pratique souvent le GM. Il sera complété pour le Raspberry Pi (E21) et la préparation sur le terrain (E22).

## Lancer le serveur

Depuis la racine du dépôt :

```bash
cd client && npm install && npm run build && cd ..
dotnet run --project src/PartyGame.Server
```

Le serveur écoute en HTTP sur le port 5000 de **toutes** les interfaces IPv4 (`0.0.0.0`), et pas seulement sur `localhost`. La console affiche `Now listening on: http://0.0.0.0:5000`. Sur le PC hôte, la page joueur reste accessible à l'adresse `http://localhost:5000/`.

## Changer de port

Le port se règle par le paramètre `Network:Port` (5000 par défaut), sans recompiler. Par ordre de priorité croissante :

| Moyen | Exemple |
|---|---|
| Fichier `appsettings.json` du serveur | `"Network": { "Port": 5001 }` |
| Variable d'environnement | `Network__Port=5001` (PowerShell : `$env:Network__Port = "5001"`) |
| Argument de ligne de commande | `dotnet run --project src/PartyGame.Server -- --Network:Port=5001` |

Si le port est déjà occupé (un autre serveur PartyGame encore lancé, une autre application), le serveur s'arrête immédiatement avec un message `FTL` qui nomme le port et rappelle comment en choisir un autre.

En développement, le serveur Vite relaie `/hub` et `/media` vers `http://localhost:5000`. Après un changement de port, définir aussi `PARTYGAME_SERVER_URL` (par exemple `http://localhost:5001`) avant `npm run dev`.

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

1. Trouver l'adresse IPv4 privée du PC : `ipconfig` dans un terminal, ligne « Adresse IPv4 » de la carte Wi-Fi ou Ethernet (par exemple `192.168.1.42`).
2. Sur le PC, ouvrir `http://localhost:5000/health` : la page doit afficher `Healthy`.
3. Sur un téléphone connecté **au même Wi-Fi**, ouvrir `http://192.168.1.42:5000/health`, puis `http://192.168.1.42:5000/` pour la page joueur.

Si le PC répond mais pas le téléphone :

- vérifier que le téléphone n'utilise pas ses données mobiles ou un autre Wi-Fi (réseau invité, répéteur isolé) ;
- vérifier le profil réseau et la règle de pare-feu ci-dessus ;
- certains Wi-Fi publics ou d'hôtel isolent les appareils entre eux : aucune configuration du PC n'y remédie. Utiliser alors un partage de connexion ou un routeur de voyage (voir E22).
