# Mode blind test

**Type d'activité :** `blindtest`
**Épopée :** [E15](../../UserStories/E15-mode-blind-test/README.md)

L'écran TV joue un extrait de chaque morceau. Le premier joueur qui buzze donne le titre et l'artiste à voix haute, et le game master juge chacun des deux depuis sa console. Le mode assemble le buzzer (E13) et l'audio de l'écran TV (E14).

> Pour l'instant, seul le descripteur est pris en charge (US-E15-01) : un pack de blind test se charge et se vérifie, mais une manche se termine dès son démarrage. Le déroulé ci-dessous arrive avec US-E15-02 à US-E15-04.

## Règles

- **Le son ne sort que de la TV.** Les téléphones ne servent qu'à buzzer et ne reçoivent aucun média.
- **Départage à l'horodatage,** comme pour les [questions buzzer](buzzer.md) : le gagnant est celui dont le doigt a touché l'écran en premier, d'après l'heure serveur de l'appui.
- **Musique en pause pendant la réponse.** Au buzz, la musique s'arrête. Après le jugement, si quelque chose reste à trouver et qu'un joueur peut encore buzzer, elle reprend où elle s'était arrêtée et le buzzer se rouvre. Le temps passé en pause ne raccourcit pas l'extrait.
- **Titre et artiste jugés séparément.** Chacun rapporte ses points (`titlePoints` et `artistPoints`, 500 par défaut). Un élément trouvé n'est plus à prendre : celui qui reste se joue entre les autres joueurs. Un morceau sans artiste ne se joue que sur le titre.
- **Un buzz par joueur et par extrait.** Un joueur qui a buzzé ne rebuzze pas sur le même extrait, qu'il ait trouvé un élément ou rien, et ne perd jamais de points.
- **Fin de l'extrait.** La musique s'arrête, mais le buzzer reste ouvert jusqu'à ce que le GM révèle la réponse : titre, artiste et visuel s'il y en a un.

## Format du descripteur

Une manche de blind test est une activité de `rounds` dont le `type` vaut `blindtest`. Le schéma `schemas/pack.schema.json` décrit chaque propriété dans VS Code et vérifie les bornes pendant la saisie.

### Manche

| Propriété | Obligatoire | Description |
|---|---|---|
| `type` | Oui | `"blindtest"`. |
| `title` | Oui | Titre de la manche, de 1 à 60 caractères, affiché à tous. |
| `titlePoints` | Non | Points de qui trouve le titre d'un morceau, de 0 à 10 000. 500 si absent. |
| `artistPoints` | Non | Points de qui trouve l'artiste d'un morceau, de 0 à 10 000. 500 si absent. |
| `tracks` | Oui | Morceaux de la manche, de 1 à 50, joués dans cet ordre. |

### Morceau

| Propriété | Obligatoire | Description |
|---|---|---|
| `excerpt` | Oui | Extrait joué sur l'écran TV (voir ci-dessous). |
| `title` | Oui | Titre du morceau, de 1 à 100 caractères : affiché au GM, qui juge les réponses données à voix haute, puis à tous à la révélation. |
| `artist` | Non | Artiste du morceau, de 1 à 100 caractères, affiché comme le titre. Sans artiste, le morceau ne se joue que sur le titre. |
| `image` | Non | Visuel montré sur la TV à la révélation, comme la pochette de l'album : chemin d'une image du pack (`.jpg`, `.jpeg`, `.png` ou `.webp`). |

### Extrait

| Propriété | Obligatoire | Description |
|---|---|---|
| `file` | Oui | Chemin d'un fichier `.mp3` du pack. Ses étiquettes ID3 (titre, artiste, pochette) sont retirées quand le serveur le sert : elles ne dévoilent pas la réponse. |
| `start` | Non | Point de départ dans le morceau, en secondes, décimales permises, avant la fin du morceau. 0 si absent. |
| `duration` | Oui | Durée de l'extrait, en secondes, de 5 à 120. Un extrait qui dépasse la fin du morceau s'arrête avec lui. |

Les chemins sont relatifs au dossier du pack, avec `/` pour séparateur, et leur casse doit être exactement celle du fichier.

### Vérifications au chargement

La structure, les bornes et l'existence des médias sont vérifiées comme pour tout pack. Chaque MP3 est lu : un fichier sans trame MP3 valide est refusé, et un extrait qui commence au-delà de la fin du morceau aussi. Le mode ajoute une vérification :

| Code | Problème | Chemin |
|---|---|---|
| `BlindTestPointsMissing` | Aucun morceau ne rapporte de points : `titlePoints` vaut 0, et `artistPoints` vaut 0 ou aucun morceau n'a d'artiste. La manche ne départagerait personne. | La manche, par exemple `$.rounds[1]`. |

Un problème rend le pack invalide, et le GM le voit avant le lancement avec le chemin concerné dans le descripteur.

### Exemple complet

Le pack [packs/blindtest-exemple](../../packs/blindtest-exemple/pack.json) suit le même modèle, avec dix airs du domaine public dont la licence est indiquée dans [LICENCE.md](../../packs/blindtest-exemple/LICENCE.md).

```json
{
  "$schema": "../../schemas/pack.schema.json",
  "formatVersion": 1,
  "title": "Blind test des classiques",
  "rounds": [
    {
      "type": "blindtest",
      "title": "Airs connus",
      "titlePoints": 500,
      "artistPoints": 300,
      "tracks": [
        {
          "excerpt": { "file": "sons/hymne-a-la-joie.mp3", "duration": 20 },
          "title": "L'Hymne à la joie",
          "artist": "Ludwig van Beethoven"
        },
        {
          "excerpt": { "file": "sons/au-clair-de-la-lune.mp3", "start": 9.6, "duration": 20 },
          "title": "Au clair de la lune"
        },
        {
          "excerpt": { "file": "sons/la-marseillaise.mp3", "duration": 20 },
          "title": "La Marseillaise",
          "artist": "Claude Joseph Rouget de Lisle",
          "image": "images/drapeau-france.png"
        }
      ]
    }
  ]
}
```
