### US-E02-03 — QR code sur l'écran TV

**Statut :** Prête

**En tant que** joueur
**je veux** scanner un QR code affiché sur la TV avec l'appareil photo de mon téléphone
**afin d'**ouvrir la page joueur sans rien taper ni rien installer

**Critères d'acceptation**
- Étant donné l'écran TV ouvert sur `/display/`, quand la page se charge, alors elle affiche un QR code qui encode `http://<adresse retenue>:<port de la page>/`, ainsi que cette même URL en clair, lisible de loin, pour une saisie manuelle.
- Étant donné un iPhone (Safari) et un téléphone Android (Chrome) sur le même Wi-Fi, quand on scanne le QR code avec l'appareil photo natif, alors la page joueur s'ouvre. La vérification est faite sur de vrais appareils, face à une TV 1080p à environ 3 m.
- Étant donné l'écran TV, quand on l'affiche sur une TV, alors le QR code et l'URL ne touchent aucun bord (marges d'au moins 5 % contre le rognage) et le QR code est en modules sombres sur fond clair, avec sa zone de silence, quel que soit le thème.
- Étant donné le serveur de développement Vite (`npm run dev`), quand on ouvre l'écran TV sur le port 5173, alors le QR code encode ce port, et le téléphone obtient la page servie par Vite.
- Étant donné l'onglet réseau des outils de développement, quand l'écran TV s'affiche, alors aucune requête ne sort du réseau local : la génération du QR code est embarquée.
- Étant donné un test E2E Playwright de l'écran TV, avec la réponse de `/api/join` simulée, quand la page s'affiche, alors un QR code et l'URL attendue sont visibles.

**Comportement en cas d'erreur**
- Public : l'écran TV n'est jamais vide. Si le serveur ne répond pas ou ne connaît aucune adresse, il affiche un message neutre (texte dans `fr.ts`), réessaie périodiquement, et affiche le QR code dès que l'information arrive.
- GM et opérateur : le problème est visible dans la console (US-E02-02).
- Joueurs : sans objet.

**Notes techniques**
- Le client compose l'URL à partir de l'adresse reçue et de `location.port`, et génère le QR code en SVG avec `uqr` (décision 2 du README). C'est une dépendance d'exécution (`dependencies`), embarquée dans le build : elle est signalée dans le commit, avec sa licence (MIT), l'absence de dépendance transitive et l'absence d'appel réseau.
- Le QR code est un composant de `client/src/shared/components/` : il sera réutilisé par le lobby (E04).
- Niveau de correction d'erreur moyen (M) : l'URL est courte, et un code moins dense reste lisible de loin.
- Le contenu encodé est vérifié par un test unitaire, quand la bibliothèque expose la matrice ou que l'URL composée est testable séparément. La lisibilité réelle est vérifiée sur appareil.
- Tous les textes (titre, invitation à scanner, message d'attente) sont dans `fr.ts`.
- Dépend de US-E02-02 (adresse retenue et endpoint) et de US-E02-04 (`/display/` servi).

**Hors périmètre**
- Liste des joueurs connectés sous le QR code (E04).
- Habillage et animations de l'écran TV (E20).
- Bouton « Démarrer » de déblocage audio (E14).
