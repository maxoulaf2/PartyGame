# Mode quiz à choix multiples

**Type d'activité :** `quiz`
**Épopée :** [E08](../../UserStories/E08-mode-quiz-qcm/README.md)

Le game master présente chaque question sur l'écran TV, ouvre les réponses, puis révèle la bonne. Les joueurs choisissent une proposition sur leur téléphone avant la fin d'un compte à rebours.

> **État de la réalisation :** le descripteur et ses vérifications sont en place (US-E08-01), ainsi que la présentation des questions (US-E08-02). L'ouverture des réponses, la révélation et le passage d'une question à l'autre arrivent avec US-E08-03 à US-E08-05 : d'ici là, une manche de quiz reste sur la présentation de sa première question.

## Règles

- **Déroulé piloté par le GM.** Le GM ouvre les réponses, révèle la bonne réponse, puis passe à la question suivante. Seul le verrouillage est automatique, à la fin du compte à rebours. Le GM peut aussi verrouiller plus tôt, par exemple quand tout le monde a répondu, ou passer une question.
- **Temps de réponse.** Chaque manche a une durée de réponse par défaut (`answerSeconds`, 20 s si absente). Une question peut la remplacer par la sienne. Les durées vont de 5 à 120 s.
- **Réponse définitive.** Un joueur ne choisit qu'une fois. Son premier choix compte, et il ne peut pas le modifier.
- **Barème.** Une bonne réponse rapporte les points de la manche (`points`, 1 000 par défaut). Si la manche définit un bonus de rapidité (`speedBonus`, 0 par défaut), il s'y ajoute en proportion du temps restant à la réception de la réponse : bonus × temps restant ÷ durée de réponse, arrondi à l'entier. Une mauvaise réponse ou une absence de réponse rapporte 0, sans pénalité. Les points sont attribués à la révélation : une question passée avant sa révélation ne rapporte rien.
- **Révélation.** L'écran TV met en évidence la bonne proposition, et montre sous chaque proposition le nombre de joueurs qui l'ont choisie et leurs pseudos, puis les joueurs sans réponse. Chaque téléphone indique à son joueur s'il a eu juste.
- **Joueurs arrivés en cours de partie.** Un joueur inscrit pendant une manche participe à toute question dont les réponses s'ouvrent après son inscription, et commence à 0 point. La règle définitive sera tranchée en phase 6.
- **Propositions.** Chaque question a de 2 à 4 propositions, dont exactement une bonne. Chacune est distinguée par une lettre (A à D), une forme (A triangle, B losange, C cercle, D carré) et une couleur, jamais par la couleur seule. L'ordre est celui du descripteur, sauf si la manche demande un mélange (`shuffleChoices`) : l'ordre mélangé, tiré à la présentation de chaque question, est alors le même sur tous les écrans.
- **Images.** Une question peut avoir une image, affichée sur l'écran TV seulement. Les téléphones n'affichent aucun média.
- **Téléphone en pavé de réponse.** Le téléphone n'affiche ni la question ni le texte des propositions : seulement un bouton par proposition, avec sa lettre, sa forme et sa couleur. Les joueurs lisent tout sur l'écran TV, et gardent la tête levée.

## Phases d'une question

| Phase | Ce qui se passe | Passage à la suivante |
|---|---|---|
| `Presentation` | La question et ses propositions s'affichent : sur la TV avec le titre de la manche, le numéro de la question (« Question 3/5 ») et son image, sur les téléphones un bouton désactivé par proposition (lettre, forme et couleur, sans texte), et sur la console GM avec la bonne réponse signalée. | Le GM ouvre les réponses. |
| `Answering` | Les joueurs répondent. Le compte à rebours tourne sur la TV et les téléphones, et la TV montre le nombre de réponses reçues. | Fin du compte à rebours, ou verrouillage par le GM. |
| `Locked` | Plus aucune réponse n'est acceptée (« Temps écoulé »). | Le GM révèle la réponse. |
| `Revealed` | La bonne réponse, la répartition des choix et les points gagnés s'affichent. | Le GM passe à la question suivante, ou termine la manche après la dernière. |

Le GM peut passer la question dans les phases `Presentation`, `Answering` et `Locked`. Seule la console GM connaît la bonne réponse avant la phase `Revealed`.

## Format du descripteur

Une manche de quiz est une activité de `rounds` dont le `type` vaut `quiz`. Le schéma `schemas/pack.schema.json` décrit chaque propriété dans VS Code et vérifie les bornes pendant la saisie.

### Manche

| Propriété | Obligatoire | Description |
|---|---|---|
| `type` | Oui | `"quiz"`. |
| `title` | Oui | Titre de la manche, de 1 à 60 caractères, affiché à tous. |
| `answerSeconds` | Non | Durée de réponse par défaut des questions, en secondes, de 5 à 120. 20 si absente. |
| `points` | Non | Points d'une bonne réponse, de 0 à 10 000. 1 000 si absent. |
| `speedBonus` | Non | Bonus de rapidité maximal, de 0 à 10 000. 0 si absent, c'est-à-dire sans bonus. |
| `shuffleChoices` | Non | `true` pour mélanger les propositions de chaque question. `false` si absent. |
| `questions` | Oui | Questions de la manche, de 1 à 50, posées dans cet ordre. |

### Question

| Propriété | Obligatoire | Description |
|---|---|---|
| `text` | Oui | Texte de la question, de 1 à 200 caractères. |
| `image` | Non | Chemin d'une image du pack, relatif au dossier du pack, avec `/` pour séparateur : `.jpg`, `.jpeg`, `.png` ou `.webp`. La casse doit être exactement celle du fichier. |
| `answerSeconds` | Non | Durée de réponse de cette question, de 5 à 120 s. Remplace celle de la manche. |
| `choices` | Oui | Propositions, de 2 à 4. |

### Proposition

| Propriété | Obligatoire | Description |
|---|---|---|
| `text` | Oui | Texte de la proposition, de 1 à 80 caractères. |
| `correct` | Non | `true` sur la bonne réponse, et sur elle seule. `false` si absent. |

### Vérifications au chargement

En plus de la structure et des bornes, le mode vérifie chaque question. Un problème rend le pack invalide, et le GM le voit avant le lancement avec le chemin concerné dans le descripteur.

| Code | Problème | Chemin signalé |
|---|---|---|
| `QuizCorrectChoiceMissing` | Aucune proposition n'a `"correct": true`. | La question, par exemple `$.rounds[0].questions[2]`. |
| `QuizCorrectChoiceDuplicated` | Plusieurs propositions ont `"correct": true`. | La question. |
| `QuizChoiceDuplicated` | Deux propositions sont identiques, sans tenir compte de la casse, des accents ni des espaces (« Le Mans » et « le  mans »). | La seconde proposition, par exemple `$.rounds[0].questions[2].choices[3]`. |

### Exemple complet

Un pack de deux manches. La première accorde un bonus de rapidité et contient une image et une question à la durée surchargée. La seconde réduit la durée de réponse et mélange les propositions. Le pack [packs/quiz-exemple](../../packs/quiz-exemple/pack.json) suit le même modèle, avec dix questions.

```json
{
  "$schema": "../../schemas/pack.schema.json",
  "formatVersion": 1,
  "title": "Soirée quiz",
  "description": "Culture générale et sciences.",
  "rounds": [
    {
      "type": "quiz",
      "title": "Culture générale",
      "points": 1000,
      "speedBonus": 500,
      "questions": [
        {
          "text": "Quel pays a ce drapeau ?",
          "image": "images/drapeau.png",
          "choices": [
            { "text": "Irlande" },
            { "text": "Italie", "correct": true },
            { "text": "Mexique" },
            { "text": "Hongrie" }
          ]
        },
        {
          "text": "Lequel de ces événements historiques est le plus ancien ?",
          "answerSeconds": 30,
          "choices": [
            { "text": "La prise de la Bastille" },
            { "text": "Le couronnement de Charlemagne", "correct": true },
            { "text": "La bataille de Marignan" }
          ]
        }
      ]
    },
    {
      "type": "quiz",
      "title": "Sciences et nature",
      "answerSeconds": 15,
      "shuffleChoices": true,
      "questions": [
        {
          "text": "La baleine est un poisson.",
          "choices": [
            { "text": "Vrai" },
            { "text": "Faux", "correct": true }
          ]
        }
      ]
    }
  ]
}
```
