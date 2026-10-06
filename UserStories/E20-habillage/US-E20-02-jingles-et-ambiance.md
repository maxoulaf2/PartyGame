### US-E20-02 — Jingles et sons d'ambiance

**Statut :** À faire

**En tant que** public
**je veux** entendre une musique d'attente dans le lobby et un jingle aux moments clés de la partie
**afin de** sentir le rythme de la soirée et savoir, même sans regarder la TV, qu'il se passe quelque chose

**Critères d'acceptation**
- Étant donné le lobby, quand la TV a débloqué son audio, alors une musique d'attente tourne en boucle, à volume modéré.
- Étant donné la partie, quand la TV affiche l'introduction d'une manche, une révélation ou le podium, alors elle joue le jingle correspondant (`roundIntro`, `reveal`, `podium`), une seule fois par écran, même si plusieurs snapshots arrivent pendant cet écran.
- Étant donné un extrait de blind test en cours, quand un écran qui a un jingle s'affiche, alors aucun jingle ne joue tant que l'extrait est en lecture ; la musique d'attente ne joue jamais en même temps qu'un extrait.
- Étant donné un pack qui définit ses propres sons, quand la TV joue un jingle ou la musique d'attente, alors elle utilise le fichier du pack ; pour un son que le pack ne définit pas, elle utilise le son embarqué.
- Étant donné un pack dont un son est introuvable ou n'est pas un MP3, quand le pack est chargé, alors il est invalide, avec le fichier et le chemin dans le descripteur, dans la console GM comme avec `validate`.
- Étant donné la console GM, quand le GM désactive « Sons d'ambiance », alors la TV ne joue plus ni jingle ni musique d'attente, les extraits de blind test restant joués ; le réglage survit à un rechargement de la TV.
- Étant donné la partie en pause (US-E19-01), quand la TV affiche la pause, alors elle coupe le jingle en cours.
- Étant donné les téléphones, quand un jingle joue sur la TV, alors ils restent silencieux.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent la validation des sons du pack, la projection `Display` des sons (identifiants de média seulement, aucun chemin), la sélection du jingle d'après les écrans en Vitest (un seul jingle par écran, aucun pendant un extrait), et la commande « Sons d'ambiance » de bout en bout.

**Comportement en cas d'erreur**
Un son illisible pendant la partie : la TV le remonte au serveur, qui le journalise en `Warning`, et continue sans le son (décision 3 du README). Rien côté joueurs, public et GM.

**Notes techniques**
- Sons embarqués dans `client/src/assets/sounds/`, licence libre (CC0 de préférence), provenance et licence documentées dans `client/src/assets/sounds/LICENSES.md`. Taille totale raisonnable (moins de 2 Mo).
- Pack : section facultative `sounds` dans `pack.json` (`lobby`, `roundIntro`, `reveal`, `podium`), chaque valeur étant le chemin d'un MP3 du pack ; régénérer `schemas/pack.schema.json` et mettre à jour `docs/guide-packs.md`.
- Projection `Display` : l'URL `/media/<identifiant>` de chaque son du pack, et l'indicateur « Sons d'ambiance ». Le réglage est un état de la partie, enregistré, modifié par une intention GM `SetAmbientSound(enabled)`, idempotente par nature.
- La TV joue jingles et musique avec un second élément `<audio>`, distinct de celui des extraits ; la détection de l'écran courant réutilise `shared/gameScreen.ts`.

**Hors périmètre**
- Un réglage de volume par son.
- Des jingles propres à chaque mode ou à chaque manche.
- Des sons sur les téléphones (contrainte du projet).
