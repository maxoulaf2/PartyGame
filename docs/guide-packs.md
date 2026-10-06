# Guide de rédaction des packs

Ce guide explique comment écrire un pack de contenu, le vérifier, le faire défiler sur la TV et le partager, sans lire le code. Le plus simple est de partir du pack [`packs/soiree-exemple`](../packs/soiree-exemple/pack.json), qui enchaîne une manche de chaque mode : copiez son dossier, renommez-le, puis remplacez les questions par les vôtres.

## Structure d'un pack

Un pack est un dossier qui contient un fichier `pack.json` (nom exact, en minuscules) et ses médias :

```
ma-soiree/
  pack.json
  images/
    tour-eiffel.jpg
  sons/
    hymne-a-la-joie.mp3
  LICENCE.md          facultatif : d'où viennent les médias et à quelles conditions ils sont réutilisables
```

Le nom du dossier est l'identifiant du pack. Le dossier se dépose dans le dossier des packs du serveur (`packs/` du dépôt avec `scripts/start.ps1`, voir [l'installation](installation.md#dossier-des-packs)). Les sous-dossiers des médias sont libres.

`pack.json` décrit le pack puis ses manches, jouées dans l'ordre :

```json
{
  "$schema": "../../schemas/pack.schema.json",
  "formatVersion": 1,
  "title": "Ma soirée",
  "description": "Facultative : quelques mots pour le GM.",
  "rounds": [
    { "type": "quiz", "title": "Échauffement", "description": "Facultative : affichée quand la manche est annoncée.", "questions": [] },
    { "type": "blindtest", "title": "Airs connus", "tracks": [] }
  ]
}
```

| Propriété | Obligatoire | Description |
|---|---|---|
| `$schema` | Non | Chemin du schéma, pour l'aide à la saisie dans VS Code (voir ci-dessous). Ignoré par le serveur. |
| `formatVersion` | Oui | Toujours `1`. |
| `title` | Oui | Titre du pack, de 1 à 60 caractères, affiché au GM au choix du pack, puis à tous pendant la partie. |
| `description` | Non | Présentation du pack pour le GM, 200 caractères au plus. |
| `rounds` | Oui | Les manches, au moins une. Le `type` de chacune désigne son mode de jeu. |

Le fichier est du JSON strict : guillemets droits `"` autour des textes et des noms de propriétés, une virgule entre deux éléments mais jamais après le dernier, pas de commentaire. Une propriété inconnue est refusée, ce qui attrape les fautes de frappe.

## Les modes de jeu

Chaque mode a sa page, avec ses règles, chacune de ses propriétés et un exemple complet de descripteur :

| `type` | Mode | Médias | Page |
|---|---|---|---|
| `quiz` | Quiz à choix multiples : de 2 à 4 propositions, une seule bonne | Image facultative par question | [quiz.md](modes/quiz.md) |
| `openquestion` | Question ouverte : réponse tapée sur le téléphone, validée par le GM | Image facultative par question | [openquestion.md](modes/openquestion.md) |
| `buzzer` | Questions buzzer : réponse orale du plus rapide, jugée par le GM | Image facultative par question | [buzzer.md](modes/buzzer.md) |
| `blindtest` | Blind test : extrait joué sur la TV, titre et artiste donnés à voix haute | Un MP3 par morceau, visuel facultatif | [blindtest.md](modes/blindtest.md) |

Un pack peut mélanger les modes et répéter un même mode dans plusieurs manches.

Chaque manche s'ouvre sur un écran d'introduction : la TV affiche son numéro (« Manche 2/4 »), son titre, le nom du mode et sa règle en deux ou trois lignes, et le GM la démarre quand il a fini de l'expliquer. Deux propriétés valent pour toutes les manches, quel que soit leur mode :

| Propriété | Obligatoire | Description |
|---|---|---|
| `title` | Oui | Titre de la manche, de 1 à 60 caractères, affiché à tous à partir de son introduction. |
| `description` | Non | Présentation de la manche, de 1 à 300 caractères, affichée sur la TV et la console GM sous la règle du mode pendant l'introduction. Sans elle, seule la règle est affichée. La règle elle-même n'est pas à écrire : elle est la même pour toutes les manches d'un mode. |

## Aide à la saisie dans VS Code

Le schéma [`schemas/pack.schema.json`](../schemas/pack.schema.json) décrit chaque propriété et ses limites. VS Code s'en sert pour compléter les noms de propriétés, afficher leur description au survol et souligner une erreur pendant la saisie.

- **Pack dans le dossier `packs/` du dépôt :** rien à faire, le dépôt associe déjà le schéma à chaque `packs/*/pack.json`.
- **Pack ailleurs :** indiquez le chemin du schéma dans la propriété `$schema`, relatif au fichier `pack.json`, par exemple `"$schema": "../PartyGame/schemas/pack.schema.json"`. Pour un pack à partager, une copie de `pack.schema.json` à côté de `pack.json` (`"$schema": "pack.schema.json"`) fonctionne partout.

Le schéma ne voit pas tout : l'existence des médias et la cohérence des données (une seule bonne réponse, par exemple) ne sont vérifiées que par `validate` et par le serveur.

## Médias

| Usage | Formats acceptés |
|---|---|
| Images (questions, visuels du blind test) | `.jpg`, `.jpeg`, `.png`, `.webp` |
| Sons (extraits du blind test) | `.mp3` |

- **Chemins relatifs au dossier du pack,** avec `/` pour séparer les dossiers, même sous Windows : `"images/tour-eiffel.jpg"`. Un média hors du dossier du pack est refusé.
- **Casse exacte.** Majuscules et minuscules doivent être exactement celles du fichier : `images/Drapeau.png` et `images/drapeau.png` sont deux fichiers différents. Windows ne fait pas la différence, mais le Raspberry Pi si : le serveur la vérifie partout, pour qu'un pack préparé sur un PC marche aussi sur le Pi. Le plus sûr est de nommer ses fichiers en minuscules, sans espace ni accent.
- **Images et sons ne s'affichent que sur la TV.** Les téléphones n'en reçoivent aucun.
- **Droits.** Pour un pack partagé, indiquez l'origine et la licence de chaque média dans un `LICENCE.md`, comme dans les packs d'exemple.

### Préparer un extrait audio

Un morceau de blind test n'a pas besoin d'être découpé : le descripteur indique où commence l'extrait et combien de temps il dure.

```json
{ "excerpt": { "file": "sons/hymne-a-la-joie.mp3", "start": 42.5, "duration": 20 }, "title": "L'Hymne à la joie" }
```

1. Convertissez le morceau en MP3 s'il ne l'est pas (Audacity, VLC ou tout logiciel de conversion). Un MP3 à débit constant (128 ou 192 kbit/s) démarre le plus précisément à la position demandée.
2. Écoutez-le dans un lecteur qui affiche la position (Audacity, VLC…) et notez la seconde où l'extrait doit commencer : `start`, décimales permises, 0 si absent.
3. Choisissez la durée de l'extrait : `duration`, de 5 à 120 secondes. Un extrait qui dépasse la fin du morceau s'arrête avec lui.
4. Inutile de retirer les étiquettes du fichier (titre, artiste, pochette) : le serveur les supprime quand il sert le MP3, elles ne dévoilent pas la réponse.

Pour vérifier que l'extrait commence au bon endroit, écoutez-le dans l'aperçu sur la TV (voir plus bas).

## Vérifier le pack avec `validate`

La commande `validate` vérifie un pack exactement comme le serveur le charge (structure, limites, médias, cohérence), sans lancer de partie. Depuis la racine du dépôt, dans PowerShell :

```powershell
dotnet run --project src/PartyGame.Server -- validate "$PWD\packs\ma-soiree"   # un pack (dossier, pack.json ou zip)
dotnet run --project src/PartyGame.Server -- validate "$PWD\packs"             # tous les packs d'un dossier
```

Donnez un chemin complet (ici avec `$PWD`) : sous `dotnet run`, un chemin relatif serait lu depuis le dossier du projet serveur. Avec l'exécutable publié, un chemin relatif au dossier courant suffit (`./PartyGame.Server validate ma-soiree`).

Un pack valide affiche ses manches :

```
Pack « Soirée d'exemple » (C:\…\packs\soiree-exemple)
  Manche 1 : Échauffement (quiz)
  Manche 2 : Réponses libres (openquestion)
  Manche 3 : Le plus rapide (buzzer)
  Manche 4 : Airs connus (blindtest)
  Pack valide
```

Un pack invalide affiche chaque problème : le fichier, le chemin dans le descripteur, le message, puis le code entre crochets. Le chemin se lit depuis la racine du fichier : `$.rounds[1].questions[2].image` est l'image de la troisième question de la deuxième manche (la numérotation commence à 0). La commande se termine avec le code 0 si tout est valide, 1 si un pack est invalide, 2 si le chemin est introuvable.

Le serveur fait les mêmes vérifications au démarrage : un pack invalide ne peut pas être choisi, et la console GM détaille ses problèmes avec « Voir les problèmes ». Après une correction, « Actualiser les packs » dans la console GM recharge les packs sans redémarrer le serveur.

## Erreurs courantes

Chaque exemple montre la ligne affichée par `validate`, puis la correction.

**Virgule en trop ou guillemet manquant**

```
pack.json, $ : JSON mal formé, ligne 5, colonne 106 : vérifiez les virgules, guillemets et accolades autour. [PackJsonInvalid]
```

Regardez la ligne indiquée et celle d'avant : une virgule après le dernier élément d'une liste (`{ … }, ]`), une virgule oubliée entre deux éléments, un guillemet typographique (`“` ou `«`) au lieu de `"`, une accolade non fermée. Le JSON mal formé masque tous les autres problèmes : corrigez-le d'abord, puis relancez `validate`.

**Faute de frappe dans un nom de propriété**

```
pack.json, $.rounds[2].questions[0].anwser : Propriété inconnue : anwser (faute de frappe ?) [PackPropertyUnknown]
pack.json, $.rounds[2].questions[0] : Propriété obligatoire absente : answer [PackPropertyMissing]
```

Corrigez le nom (`answer`). Les noms sont en anglais, sensibles à la casse, et listés dans la page de chaque mode.

**Type de manche inconnu**

```
pack.json, $.rounds[0].type : Type d'activité inconnu : « quizz » [PackRoundTypeUnknown]
```

Le `type` vaut exactement `quiz`, `openquestion`, `buzzer` ou `blindtest`.

**Valeur du mauvais type**

```
pack.json, $.rounds[1].questions[1].choices[0].correct : Valeur incorrecte : il faut true ou false. [PackValueTypeInvalid]
```

`"correct": "true"` est un texte : écrivez `"correct": true`, sans guillemets. De même, un nombre s'écrit sans guillemets (`"duration": 20`).

**Valeur hors limites**

```
pack.json, $.rounds[3].tracks[2].excerpt.duration : Valeur hors limites : de 5 à 120 [PackValueOutOfRange]
```

Ramenez la valeur dans les limites indiquées. Les textes et les listes ont aussi les leurs (« Texte trop long : longueur maximale 100 », « Nombre d'éléments incorrect : de 2 à 4 »).

**Question de quiz sans bonne réponse**

```
pack.json, $.rounds[0].questions[0] : Question sans bonne réponse : marquez une proposition avec "correct": true [QuizCorrectChoiceMissing]
```

Ajoutez `"correct": true` à la bonne proposition, et à une seule.

**Média introuvable**

```
pack.json, $.rounds[1].questions[2].image : Média introuvable : images/tour-eiffel.jpg [PackMediaMissing]
```

Vérifiez que le fichier est bien dans le dossier du pack, au chemin indiqué, avec la bonne extension (`.jpg` et `.jpeg` ne sont pas interchangeables).

**Majuscules et minuscules différentes**

```
pack.json, $.rounds[1].questions[1].image : Majuscules et minuscules différentes du fichier : images/drapeau.png au lieu de images/Drapeau.png [PackMediaCaseMismatch]
```

Écrivez le chemin avec la casse du fichier, ou renommez le fichier. Sous Windows, renommer un fichier sans changer que sa casse demande deux étapes (`Drapeau.png` → `x.png` → `drapeau.png`).

**Format de média non pris en charge**

```
pack.json, $.rounds[3].tracks[2].excerpt.file : Format de média non pris en charge : sons/air.wav (il faut un fichier .mp3) [PackMediaTypeUnsupported]
```

Convertissez le fichier dans un format accepté (voir « Médias »).

**MP3 illisible**

```
pack.json, $.rounds[3].tracks[1].excerpt.file : Fichier MP3 illisible : sons/vide.mp3 [PackMediaUnreadable]
```

Le fichier est vide, tronqué, ou n'est pas un vrai MP3 (un autre format renommé en `.mp3`). Reconvertissez-le depuis l'original.

**Extrait qui commence après la fin du morceau**

```
pack.json, $.rounds[3].tracks[0].excerpt.start : L'extrait commence à 600 s, après la fin du morceau (35.9 s) [PackAudioExcerptStartBeyondEnd]
```

`start` est en secondes : 2 min 30 s s'écrit `150`. Vérifiez aussi que `file` désigne le bon morceau.

## Aperçu sur la TV

Avant la soirée, faites défiler le pack sur l'écran TV pour relire chaque question telle que le public la verra, réponse comprise, et écouter chaque extrait : dans le lobby de la console GM, « Aperçu » à côté du pack. La console avance et recule d'une question, choisit une manche et joue l'extrait d'un blind test sur la TV. Les téléphones restent sur le lobby et ne voient rien. Une image ou un extrait que la TV n'arrive pas à charger apparaît dans les incidents de la console. Le détail est dans [l'installation](installation.md#aperçu-dun-pack-sur-la-tv).

Après une modification du pack : « Actualiser les packs », puis relancer l'aperçu.

## Partager le pack en zip

Un pack se partage en un seul fichier : compressez son dossier (clic droit, « Compresser » sous Windows ou macOS), puis déposez le `.zip` tel quel dans le dossier des packs du serveur, sans le décompresser. Le `pack.json` doit se trouver à la racine du zip, ou dans un unique dossier à sa racine, ce que produit « Compresser ». L'identifiant du pack est le nom du zip sans `.zip` : un dossier et un zip du même nom dans le dossier des packs sont signalés en conflit, retirez l'un des deux.

`validate` vérifie aussi un zip : vérifiez le zip lui-même avant de l'envoyer, pour vous assurer qu'il contient tous les médias.

```powershell
dotnet run --project src/PartyGame.Server -- validate "$PWD\ma-soiree.zip"
```
