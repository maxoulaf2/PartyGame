### US-E13-01 — Bouton buzzer du téléphone

**Statut :** À faire

**En tant que** joueur
**je veux** un gros bouton qui réagit à l'instant où je le touche
**afin que** mon buzz compte au moment où j'ai appuyé, quel que soit mon téléphone ou le Wi-Fi

**Critères d'acceptation**
- Étant donné un buzzer ouvert, quand le joueur pose le doigt sur le bouton, alors l'appui est pris en compte sur `pointerdown`, sans attendre que le doigt se lève. Le bouton passe aussitôt à « Buzz envoyé », en attente de confirmation (interface optimiste).
- Étant donné un appui, quand le téléphone l'envoie, alors il porte l'instant de l'appui en heure serveur : `performance.now()` capturé dans le gestionnaire `pointerdown`, converti avec l'estimation d'horloge de `shared/connection` (`ClockSync`), et non l'heure d'envoi.
- Étant donné un joueur qui appuie plusieurs fois, ou avec plusieurs doigts, quand le buzzer est ouvert, alors un seul buzz part pour cette ouverture.
- Étant donné un buzzer fermé, un joueur bloqué ou un téléphone pas encore resynchronisé (pas de snapshot frais, ou première salve de synchronisation d'horloge pas terminée), quand le joueur appuie, alors rien n'est envoyé et le bouton est visiblement désactivé.
- Étant donné le bouton, quand il s'affiche, alors il occupe l'essentiel de l'écran, reste utilisable d'une main, et porte `touch-action: manipulation` ; aucun zoom, sélection de texte ou menu contextuel ne se déclenche sur un appui long (iOS et Android).
- Étant donné les états du bouton (fermé, ouvert, envoyé, gagné, perdu, bloqué), quand ils s'affichent, alors ils se distinguent par le texte et la forme, pas seulement par la couleur. Les textes sont dans `fr.ts`.
- Étant donné les tests, quand ils s'exécutent, alors Vitest couvre la conversion de l'instant d'appui en heure serveur et l'envoi unique par ouverture, et Playwright l'appui sur iPhone (WebKit) et Pixel (Chromium).

**Comportement en cas d'erreur**
Joueur : si la connexion tombe après l'appui, le buzz reste dans la file d'envoi et repart à la reconnexion avec son horodatage d'origine ; le serveur le juge (US-E13-02). Public et GM : rien.

**Notes techniques**
- Composant `shared/components/BuzzerButton.svelte`, sans logique de jeu : il reçoit son état (`closed`, `open`, `blocked`…) de la vue du mode, et appelle un rappel avec l'instant d'appui en heure serveur. Le mode construit son intention (`buzzer.buzz`, `blindtest.buzz`), qui passe par la file d'envoi (`ClientSeq`).
- `ClockSync` expose la conversion d'un instant `performance.now()` en heure serveur (elle existe pour les comptes à rebours dans l'autre sens).
- Aucune vibration : `navigator.vibrate` n'existe pas sur Safari iOS. Aucun son : les téléphones ne jouent jamais d'audio.

**Hors périmètre**
- L'arbitrage (US-E13-02) et l'annonce du gagnant (US-E13-04).
- Un buzzer physique ou un clavier.
