# E14 — Audio sur l'écran TV

**Phase :** 4. Buzzer et blind test
**Objectif :** un son maîtrisé. Seul l'écran TV joue de l'audio. Il lit un extrait défini dans le pack, à partir d'un point précis du morceau, à l'instant décidé par le serveur, et il le reprend au bon endroit après un rechargement. Un fichier illisible ne se voit pas à l'écran : seul le GM est prévenu. Le fichier servi ne dévoile pas le titre du morceau à qui l'inspecterait.

## User stories

| US | Titre | Statut | Dépend de |
|---|---|---|---|
| [US-E14-01](US-E14-01-deverrouillage-audio.md) | Déverrouillage de l'audio sur l'écran TV | Terminée | — |
| [US-E14-02](US-E14-02-extraits-et-fichiers-audio.md) | Fichiers audio et extraits dans les packs | Terminée | — |
| [US-E14-03](US-E14-03-lecture-synchronisee.md) | Lecture d'un extrait à l'instant décidé par le serveur | À faire | US-E14-01, US-E14-02 |
| [US-E14-04](US-E14-04-echec-de-lecture.md) | Échec de lecture d'un média audio | À faire | US-E14-03 |

US-E14-03 n'est vérifiable de bout en bout qu'avec un mode qui joue de l'audio : elle se réalise avec US-E15-02.

## Décisions

Toutes les décisions qui bloquaient l'épopée sont prises (2026-10-05). Les valeurs numériques ci-dessous sont des valeurs de départ, ajustables sans nouvelle décision.

1. **Lecture par un élément `<audio>` :** l'écran TV lit les fichiers en streaming, avec des requêtes partielles (Range) : un extrait qui commence au milieu d'un morceau ne télécharge que ce qu'il lit. Le déclenchement se fait à quelques dizaines de millisecondes près, ce qui suffit puisque tout le monde entend la même TV : l'équité du buzzer tient à l'horodatage des appuis, pas au son. La pause et la reprise sont natives. Option écartée : la Web Audio API, précise à la milliseconde, mais qui télécharge et décode le fichier entier (lourd sur le Pi) et impose de gérer pause et reprise à la main.
2. **Lecture décrite dans le snapshot :** la projection `Display` d'un mode qui joue de l'audio décrit la lecture en cours : média, position de départ dans le fichier, instant de déclenchement en heure serveur, ou position de pause. La TV en déduit à tout moment ce qu'elle doit jouer, y compris après un rechargement ou une reprise sur crash, où elle reprend à la position courante. Option écartée : un effet `PlayAudio` qui envoie un ordre ponctuel à la TV, à rejouer à chaque reconnexion. L'[ADR 0001](../../docs/adr/0001-architecture-generale.md), qui citait `PlayAudio` comme exemple d'effet, est mis à jour avec US-E14-03.
3. **MP3 seulement, servis sans métadonnées :** les packs n'acceptent que des fichiers `.mp3` pour l'audio. Les étiquettes ID3 (titre, artiste, pochette) en sont retirées à la volée quand le serveur les sert : n'importe qui peut ouvrir les outils de développement de la TV et télécharger le fichier, qui ne doit pas dévoiler la réponse d'un blind test. Option écartée : laisser aux auteurs de packs le soin de nettoyer leurs fichiers.
4. **Démarrage avec une avance :** un ordre de lecture est fixé 500 ms après sa décision par le serveur, pour que la TV ait reçu le snapshot et positionné le média préchargé.

## Ordre de réalisation suggéré

1. US-E14-01 et US-E14-02, en parallèle de E13.
2. US-E14-03 avec US-E15-02, puis US-E14-04.

## Critère de sortie (phase 4)

Voir le README de [E15](../E15-mode-blind-test/README.md#critère-de-sortie-phase-4).
