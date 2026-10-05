### US-E14-02 — Fichiers audio et extraits dans les packs

**Statut :** Terminée

**En tant qu'** auteur de pack
**je veux** référencer des MP3 et y définir des extraits par un point de départ et une durée
**afin de** faire écouter le passage le plus reconnaissable d'un morceau, avec la garantie qu'il est lisible avant la partie

**Critères d'acceptation**
- Étant donné un descripteur, quand un champ attend un fichier audio, alors seuls les fichiers `.mp3` du pack sont acceptés ; un champ image n'accepte que les images, et un champ audio que des MP3 (`PackMediaTypeUnsupported` sinon).
- Étant donné un extrait, quand il est décrit, alors il a un fichier audio, un point de départ en secondes (`start`, 0 par défaut, décimales permises) et une durée en secondes (`duration`, de 5 à 120). Ce type commun (`AudioExcerpt` dans `PartyGame.Contracts.Packs`) sert à tous les modes.
- Étant donné un MP3, quand le pack est chargé, alors sa durée est lue et un extrait qui commence au-delà de la fin du morceau est refusé avec un message précis. Un extrait qui dépasse la fin est accepté : il s'arrête avec le morceau.
- Étant donné un fichier `.mp3` qui n'est pas un MP3 lisible (aucune trame valide), quand le pack est chargé, alors il est refusé (`PackMediaUnreadable`).
- Étant donné un MP3 avec des étiquettes ID3v1 ou ID3v2, quand le serveur le sert sous `/media/<identifiant>`, alors elles en sont retirées, y compris pour une requête partielle (Range) : aucun titre, artiste ni pochette n'est téléchargeable (décision 3 du README).
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent la lecture de la durée (débit constant et variable), le refus d'un extrait au-delà de la fin et d'un fichier illisible, et le service sans étiquettes avec et sans Range.

**Comportement en cas d'erreur**
Contenu invalide : détecté au chargement, partie non lançable, problème présenté au GM avec le fichier, le chemin dans le descripteur et sa cause.

**Notes techniques**
- Lecture de la durée et des bornes des trames dans `PartyGame.Content`, sans dépendance : en-tête Xing/Info ou VBRI s'il existe, sinon parcours des en-têtes de trames. Les bornes (début et fin des données audio, hors ID3) sont gardées avec le média, pour le service.
- `PackMediaFiles` sert la plage des données audio comme un fichier à part entière (en-têtes `Content-Length` et `Content-Range` recalculés).
- Le type attendu d'un média devient une propriété de sa référence (image ou audio) plutôt qu'une liste unique d'extensions (`MediaCheck`).
- Régénérer `schemas/pack.schema.json`.
- Des MP3 courts de test, générés ou libres de droits, sont ajoutés aux packs de test.
- Réalisation : `AudioExcerpt` (`file`, `start` de 0 à 3 600, `duration` de 5 à 120) et l'attribut `[AudioFile]`, qui marque un `MediaPath` audio, dans `PartyGame.Contracts.Packs`. `Mp3File` lit la durée (Xing/Info, VBRI, sinon parcours des trames ; MPEG couche III seulement) et `Mp3Audio` les bornes des données audio. Codes ajoutés : `PackMediaUnreadable` et `PackAudioExcerptStartBeyondEnd` (`start`, `duration`) ; `PackMediaTypeUnsupported` gagne le paramètre `expected` (`image` ou `audio`).
- Écarts : les bornes ne sont pas gardées dans l'état mais relues à chaque requête (`Mp3Audio.Find`, quelques octets à chaque bout du fichier), ce qui évite de changer l'état persisté. Aucun mode n'utilise encore `AudioExcerpt` : il n'apparaît pas dans `schemas/pack.schema.json`, les contrôles sont testés sur un extrait lu seul et le service sur un fichier hors partie, et les MP3 de test (générés par `tests/Shared/Audio/Mp3Samples`) rejoindront les packs avec US-E15-01. Les étiquettes APE ne sont pas retirées.

**Hors périmètre**
- D'autres formats audio (AAC, OGG).
- Le fondu en début et en fin d'extrait (habillage, E20).
