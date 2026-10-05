# Mode questions buzzer

**Type d'activité :** `buzzer`
**Épopée :** [E13](../../UserStories/E13-buzzer/README.md)

Le game master pose une question, qui s'affiche sur l'écran TV pendant que le buzzer s'ouvre sur les téléphones. Le premier joueur qui buzze répond à voix haute, et le GM juge sa réponse depuis sa console.

> **État de la réalisation :** le GM pose la première question et le gagnant du buzzer obtient la main (US-E13-04). Le jugement, la réouverture du buzzer, la révélation et le passage aux questions suivantes arrivent avec US-E13-05 : d'ici là, la manche reste sur sa première question.

## Règles

- **Question affichée à l'ouverture du buzzer.** Le GM pose la question : la TV l'affiche, avec son image s'il y en a une, et le buzzer s'ouvre sur tous les téléphones. Le GM peut aussi la lire à voix haute. La réponse attendue n'apparaît que sur la console GM, jusqu'à la révélation.
- **Départage à l'horodatage.** Le gagnant est celui dont le doigt a touché l'écran en premier, d'après l'heure serveur de l'appui, et non d'après l'arrivée de son message. Le serveur attend une fenêtre d'arbitrage après le premier buzz reçu (250 ms par défaut, `Buzzer:ArbitrationMilliseconds`).
- **Réponse orale, jugée par le GM.** Le joueur qui a la main répond à voix haute. Le téléphone ne sert qu'à buzzer.
- **Mauvaise réponse.** Le joueur est bloqué pour la question, sans perdre de points, et le buzzer se rouvre aux autres.
- **Barème.** Une bonne réponse rapporte les points de la manche (`points`, 1 000 par défaut), attribués à la révélation.
- **Images.** Une question peut avoir une image, affichée sur l'écran TV avec la question. Les téléphones n'affichent aucun média.

## Phases d'une question

| Phase | TV | Téléphones | Console GM |
|---|---|---|---|
| `Ready` : question annoncée | « Question n » | Buzzer fermé | Texte, réponse attendue, bouton « Poser la question » |
| `Open` : buzzer ouvert (`buzzer.askQuestion`) | Question et image | Buzzer ouvert | « Buzzer ouvert : en attente d'un buzz » |
| `Arbitrating` : fenêtre d'arbitrage en cours | Comme `Open` | Comme `Open` ; « Buzz envoyé… » pour qui a buzzé | Comme `Open` |
| `Answering` : un gagnant a la main | Question et « Pseudo a la main » | « À toi de répondre ! » pour le gagnant, son pseudo pour les autres | « Pseudo a la main » |

La fenêtre d'arbitrage reste invisible : les projections montrent `Open` tant que le gagnant n'est pas désigné, et aucune ne contient les horodatages des buzz. Un buzz (`buzzer.buzz`) nomme la question et l'ouverture du buzzer : renvoyé après une reconnexion, il ne compte jamais pour une ouverture suivante. Un joueur arrivé pendant la question peut buzzer.

## Format du descripteur

Une manche de questions buzzer est une activité de `rounds` dont le `type` vaut `buzzer`. Le schéma `schemas/pack.schema.json` décrit chaque propriété dans VS Code et vérifie les bornes pendant la saisie.

### Manche

| Propriété | Obligatoire | Description |
|---|---|---|
| `type` | Oui | `"buzzer"`. |
| `title` | Oui | Titre de la manche, de 1 à 60 caractères, affiché à tous. |
| `points` | Non | Points d'une bonne réponse, de 0 à 10 000. 1 000 si absent. |
| `questions` | Oui | Questions de la manche, de 1 à 50, posées dans cet ordre. |

### Question

| Propriété | Obligatoire | Description |
|---|---|---|
| `text` | Oui | Texte de la question, de 1 à 200 caractères. |
| `answer` | Oui | Réponse attendue, de 1 à 100 caractères : affichée au GM, qui juge les réponses données à voix haute, puis à tous à la révélation. |
| `image` | Non | Chemin d'une image du pack, relatif au dossier du pack, avec `/` pour séparateur : `.jpg`, `.jpeg`, `.png` ou `.webp`. La casse doit être exactement celle du fichier. |

### Vérifications au chargement

La structure, les bornes et l'existence des images sont vérifiées comme pour tout pack. Le mode n'ajoute aucune vérification : chaque question est indépendante, et le GM juge les réponses à l'oral. Un problème rend le pack invalide, et le GM le voit avant le lancement avec le chemin concerné dans le descripteur.

### Exemple complet

Le pack [packs/buzzer-exemple](../../packs/buzzer-exemple/pack.json) suit le même modèle, avec dix questions.

```json
{
  "$schema": "../../schemas/pack.schema.json",
  "formatVersion": 1,
  "title": "Soirée buzzer",
  "rounds": [
    {
      "type": "buzzer",
      "title": "Le plus rapide",
      "points": 500,
      "questions": [
        { "text": "Qui a peint La Joconde ?", "answer": "Léonard de Vinci" },
        { "text": "De quel pays est ce drapeau ?", "answer": "L'Italie", "image": "images/drapeau.png" }
      ]
    }
  ]
}
```
