### US-E02-01 — Écoute sur le réseau local

**Statut :** Terminée

**En tant qu'**opérateur
**je veux** que le serveur soit joignable depuis tous les appareils du réseau local dès son lancement
**afin de** ne rien configurer d'autre que le Wi-Fi avant une soirée

**Critères d'acceptation**
- Étant donné un serveur lancé par `dotnet run --project src/PartyGame.Server` sans configuration particulière, quand on inspecte ses points d'écoute, alors il écoute en HTTP sur le port 5000 de toutes les interfaces IPv4 (`0.0.0.0`) et non seulement sur `localhost`.
- Étant donné le PC hôte et un téléphone sur le même Wi-Fi, quand le téléphone ouvre `http://<IP du PC>:5000/`, alors la page joueur s'affiche.
- Étant donné le PC hôte lui-même, quand on ouvre `http://localhost:5000/`, alors la page joueur s'affiche toujours.
- Étant donné `appsettings.json`, quand on change le port d'écoute dans la configuration (ou par une variable d'environnement), alors le serveur écoute sur ce port, sans recompiler.
- Étant donné le port 5000 déjà occupé, quand on lance le serveur, alors il s'arrête immédiatement avec un message `Fatal` qui nomme le port et indique comment en choisir un autre, au lieu d'une pile d'appels brute.
- Étant donné la documentation d'installation, quand un opérateur la lit, alors il sait autoriser le port dans le pare-feu Windows (profil réseau « Privé », commande `netsh` ou invite de première exécution) et comment vérifier qu'un téléphone atteint bien le serveur.

**Comportement en cas d'erreur**
Joueurs et public : si le pare-feu bloque le port, le téléphone n'atteint pas la page, et rien d'autre ne peut être fait de ce côté. Opérateur : un port occupé arrête le serveur avec un message clair dès le démarrage. Il n'existe aucun cas d'échec silencieux après le démarrage.

**Notes techniques**
- L'écoute est configurée par la configuration Kestrel (`Kestrel:Endpoints` ou `Urls`) dans `appsettings.json`, et non codée en dur. Le dépôt n'a pas de `launchSettings.json` : ne pas en ajouter un qui imposerait `localhost`.
- Le port effectif doit être accessible aux autres services (bannière de démarrage, US-E02-02) par une option typée, plutôt que relu dans la chaîne d'URL.
- Le test d'intégration démarre le vrai hôte Kestrel sur un port libre et vérifie qu'il répond sur une adresse non loopback de la machine (`WebApplicationFactory` utilise `TestServer` et ne prouve rien sur l'écoute).
- Documentation d'installation de l'opérateur à créer en français dans `docs/` (par exemple `docs/installation.md`) : elle sera enrichie par E21 et E22.
- Mettre à jour la section « Commandes » et les « Points d'attention » de CLAUDE.md si le mécanisme de configuration du port est nouveau.

**Hors périmètre**
- HTTPS (reporté, voir CLAUDE.md).
- Ajout automatique d'une règle de pare-feu par le serveur, qui exigerait des droits administrateur.
- Écoute IPv6 : le QR code n'encode que de l'IPv4.
- Service systemd et démarrage automatique sur le Raspberry Pi (E21).
