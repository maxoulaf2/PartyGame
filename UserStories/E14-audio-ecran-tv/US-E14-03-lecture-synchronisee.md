### US-E14-03 — Lecture d'un extrait à l'instant décidé par le serveur

**Statut :** Terminée

**En tant que** public
**je veux** entendre l'extrait démarrer quand le jeu le décide, s'arrêter net au buzz et reprendre au même endroit
**afin que** le son suive exactement le déroulé de la partie

**Critères d'acceptation**
- Étant donné une étape d'un mode qui annonce un extrait, quand la TV reçoit le snapshot, alors elle précharge le média et se positionne sur le point de départ avant tout ordre de lecture.
- Étant donné un ordre de lecture (média, position, instant de déclenchement en heure serveur), quand l'instant arrive, alors la TV démarre la lecture à la position demandée, à 50 ms près, d'après son horloge synchronisée.
- Étant donné une lecture mise en pause par le serveur (position de pause), quand la TV reçoit le snapshot, alors elle s'arrête aussitôt et se positionne sur la position de pause ; une reprise repart de cette position.
- Étant donné la TV rechargée ou reconnectée pendant une lecture, quand le snapshot arrive, alors elle reprend à la position courante (position de départ + temps écoulé depuis le déclenchement), sans repartir du début.
- Étant donné la fin de l'extrait (durée atteinte), quand la TV y arrive, alors elle s'arrête d'elle-même, même sans nouveau snapshot.
- Étant donné les téléphones, quand un extrait se joue, alors ils ne jouent aucun son et leurs projections ne contiennent aucun média audio.
- Étant donné les tests, quand ils s'exécutent, alors Vitest couvre le calcul de la position courante (avant, pendant et après l'extrait, en pause), et Playwright la lecture, la pause et la reprise après rechargement sur la TV (avec US-E15-02).

**Comportement en cas d'erreur**
Public : si le média n'est pas prêt à l'instant prévu, la lecture démarre dès qu'il l'est, à la position courante ; l'écran reste inchangé. GM : rien, sauf échec (US-E14-04).

**Notes techniques**
- Contrat commun `AudioPlayback` (identifiant du média, position de départ, instant de déclenchement ou position de pause, fin de l'extrait), porté par la vue `Display` des modes qui jouent de l'audio (décision 2 du README).
- Côté moteur, un instant de déclenchement vaut `context.Now` + 500 ms (décision 4 du README). La position de pause se calcule par le moteur à partir de l'instant de déclenchement et de `context.Now`.
- Côté client, un module `shared/audio` (un seul élément `<audio>` pour la page TV) réconcilie l'état demandé avec l'état de l'élément à chaque snapshot et à chaque resynchronisation d'horloge. Les vues des modes lui passent l'`AudioPlayback` de leur snapshot.
- `ResumeRound` des modes : l'instant de déclenchement est décalé comme les autres échéances, la position reste celle du dernier enregistrement.

- Réalisation : `AudioPlayback` (`url`, `position`, `end`, `startsAt`) dans `PartyGame.Contracts`, absent de toute projection `Player`. Côté moteur, `ExcerptPlayback` (`PartyGame.Engine/Audio`) : `Play` fixe le départ à `context.Now` + 500 ms (`Lead`), `Pause` calcule la position à `context.Now`, bornée à la fin de l'extrait, `Resume` décale le départ. Côté client, `playbackAt` (`shared/audio/playbackAt.ts`, couvert par Vitest) donne la position à un instant serveur, et `ExcerptPlayer` (`shared/audio/excerptPlayer.ts`) pilote un seul `<audio>` créé à la première utilisation : il se positionne, démarre et s'arrête de lui-même par des minuteries, et se recale (au-delà de 50 ms d'écart) à chaque snapshot, à chaque resynchronisation d'horloge et quand le fichier commence enfin à jouer. Une lecture refusée parce que l'audio est verrouillé reprend au clic sur « Démarrer », à la position courante. E2E : `e2e/blindtest.spec.ts`.
- Écart : la précision de 50 ms au déclenchement reste à mesurer sur la vraie TV (critère de sortie de la phase 4).

**Hors périmètre**
- La lecture sur plusieurs enceintes ou plusieurs écrans TV synchronisés.
