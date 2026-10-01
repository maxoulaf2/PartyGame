### US-E02-02 — Détection de l'adresse IPv4 privée

**Statut :** Terminée

**En tant qu'**opérateur
**je veux** que le serveur détermine seul l'adresse à laquelle les téléphones peuvent le joindre, en me laissant la corriger si plusieurs réseaux sont actifs
**afin que** le QR code affiché sur la TV mène toujours au bon endroit

**Critères d'acceptation**
- Étant donné un PC connecté à un seul réseau Wi-Fi ou Ethernet, quand le serveur démarre, alors l'adresse retenue est l'IPv4 privée de cette interface (plages `10.0.0.0/8`, `172.16.0.0/12` ou `192.168.0.0/16`).
- Étant donné des interfaces inactives, de loopback, en lien local (`169.254.0.0/16`), en CGNAT ou VPN maillé (`100.64.0.0/10`), ou virtuelles (Hyper-V `vEthernet`, WSL, Docker, VirtualBox, VMware), quand le serveur choisit une adresse, alors il les écarte.
- Étant donné plusieurs interfaces candidates, quand le serveur choisit, alors il privilégie celle qui a une passerelle par défaut, et son choix est déterministe : il ne change pas d'un démarrage à l'autre si le réseau ne change pas.
- Étant donné une adresse imposée par la configuration (`Network:AdvertisedAddress`), quand le serveur démarre, alors elle est retenue telle quelle, même si elle ne fait pas partie des candidates. Un avertissement est journalisé si elle n'appartient à aucune interface active.
- Étant donné le serveur démarré, quand l'opérateur lit la console, alors il voit une bannière qui donne l'adresse retenue, les URL complètes de l'écran TV (`/display/`) et de l'interface GM (`/gm/`), et la liste des autres candidates avec la façon d'en imposer une.
- Étant donné l'écran TV, quand il demande les informations de connexion au serveur, alors il reçoit l'adresse retenue, et uniquement elle : ni la liste des interfaces, ni le code GM.
- Étant donné une liste d'interfaces simulée (Wi-Fi + `vEthernet` + VPN, Ethernet + Wi-Fi, aucune interface…), quand la sélection s'exécute en test unitaire, alors l'adresse retenue est celle attendue pour chaque cas.

**Comportement en cas d'erreur**
- Aucune IPv4 privée trouvée (PC hors réseau) : le serveur démarre quand même et journalise un avertissement qui invite à connecter le PC au Wi-Fi. L'écran TV n'est pas vide : il affiche un message neutre (texte dans `fr.ts`) à la place du QR code.
- Joueurs : sans objet, ils n'ont pas encore rejoint.

**Notes techniques**
- La sélection est une fonction pure (liste d'interfaces → adresse retenue et candidates), testée sans réseau. L'accès à `NetworkInterface.GetAllNetworkInterfaces()` est isolé derrière une abstraction injectée.
- Le filtrage des interfaces virtuelles par nom est une heuristique. On la complète avec le type d'interface et la présence d'une passerelle, et on la documente dans le code.
- Les informations de connexion transitent par un DTO de `PartyGame.Contracts` (par exemple `JoinInfo`, avec une adresse nullable), servi par un endpoint HTTP en lecture seule (par exemple `GET /api/join`). Les types TypeScript sont régénérés (`npm run generate:contracts`). Le hub n'existe pas encore (E03) : l'information pourra rejoindre le snapshot `Display` à ce moment-là.
- `/api` est ajouté au proxy Vite (`server.proxy`, repris par `vite preview`).
- La bannière est destinée à l'opérateur : elle est écrite en français directement sur la console, et non par le logger. Les logs restent en anglais.
- Le choix depuis l'interface GM est reporté à E04 (décision 1 du README).

**Hors périmètre**
- Choix de l'interface depuis l'interface GM (E04).
- Détection d'un changement de réseau après le démarrage : l'adresse est calculée une fois, et un redémarrage suffit.
- Diagnostic de l'isolation des clients sur le Wi-Fi (E12).
