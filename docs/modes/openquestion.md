# Mode question ouverte

**Type d'activité :** `openquestion`
**Épopée :** [E16](../../UserStories/E16-mode-question-ouverte/README.md)

Le game master affiche une question sur l'écran TV. Chaque joueur tape sa réponse sur son téléphone avant la fin d'un compte à rebours. Le serveur pré-classe les réponses, le GM valide le lot d'un coup d'œil, puis la TV révèle la bonne réponse avec tout ce que les joueurs ont proposé.

Pour l'instant, seuls le descripteur et ses vérifications existent (US-E16-01) : une manche de questions ouvertes se termine dès qu'elle commence. Le déroulé arrive avec US-E16-02 à US-E16-04.

## Règles

- **Compte à rebours et réponse définitive.** Chaque question a une durée de réponse (`answerSeconds`). Un joueur n'envoie qu'une réponse, qu'il ne peut plus corriger.
- **Comparaison tolérante.** Avant d'être comparée à la réponse attendue et à ses variantes, une réponse est normalisée : minuscules, sans accents, ponctuation et symboles remplacés par des espaces, espaces superflus retirés, et article initial ôté (le, la, les, l', un, une, des, the). « L'Italie ! » et « italie » sont donc la même réponse. Les fautes de frappe sont tolérées selon la longueur de la réponse attendue, sauf pour une question numérique.
- **Points.** Une réponse acceptée rapporte les points de la manche (`points`), plus un bonus de rapidité facultatif (`speedBonus`) en proportion du temps restant.

## Format du descripteur

Une manche de questions ouvertes est une activité de `rounds` dont le `type` vaut `openquestion`. Le schéma `schemas/pack.schema.json` décrit chaque propriété dans VS Code et vérifie les bornes pendant la saisie.

### Manche

| Propriété | Obligatoire | Description |
|---|---|---|
| `type` | Oui | `"openquestion"`. |
| `title` | Oui | Titre de la manche, de 1 à 60 caractères, affiché à tous. |
| `answerSeconds` | Non | Durée de réponse par défaut des questions, en secondes, de 5 à 120. 30 si absente. |
| `points` | Non | Points d'une bonne réponse, de 0 à 10 000. 1 000 si absent. |
| `speedBonus` | Non | Bonus de rapidité maximal, de 0 à 10 000, ajouté en proportion du temps restant. 0 si absent. |
| `maxLength` | Non | Longueur maximale d'une réponse tapée, en caractères, de 5 à 100. 40 si absente. |
| `questions` | Oui | Questions de la manche, de 1 à 50, posées dans cet ordre. |

### Question

| Propriété | Obligatoire | Description |
|---|---|---|
| `text` | Oui | Texte de la question, de 1 à 200 caractères. |
| `answer` | Oui | Réponse attendue, de 1 caractère à `maxLength`, affichée à la révélation. |
| `acceptedAnswers` | Non | Autres réponses acceptées, de 0 à 20, chacune de 1 caractère à `maxLength`. Inutile d'y répéter une variante de casse, d'accents ou d'article : la normalisation s'en charge. |
| `inputMode` | Non | Clavier des téléphones : `"text"` (par défaut) ou `"numeric"`, pour un nombre entier écrit en chiffres, comparé sans tolérance. |
| `answerSeconds` | Non | Durée de réponse de cette question, de 5 à 120 secondes, à la place de celle de la manche. |
| `image` | Non | Chemin d'une image du pack, relatif au dossier du pack, avec `/` pour séparateur : `.jpg`, `.jpeg`, `.png` ou `.webp`. Affichée sur la TV avec la question. |

### Vérifications au chargement

La structure, les bornes et l'existence des images sont vérifiées comme pour tout pack. Le mode vérifie en plus, pour chaque question :

- que la réponse attendue et chaque variante tiennent dans `maxLength`, et qu'aucune variante n'est vide ;
- qu'aucune n'est vide une fois normalisée (« ! », « Les ») : aucune réponse ne pourrait lui correspondre ;
- qu'aucune variante ne revient, une fois normalisée, à la réponse attendue ou à une autre variante ;
- pour une question `numeric`, que la réponse attendue et ses variantes ne contiennent que des chiffres.

Un problème rend le pack invalide, et le GM le voit avant le lancement avec le chemin concerné dans le descripteur.

### Exemple complet

Le pack [packs/openquestion-exemple](../../packs/openquestion-exemple/pack.json) suit le même modèle, avec dix questions.

```json
{
  "$schema": "../../schemas/pack.schema.json",
  "formatVersion": 1,
  "title": "Soirée questions ouvertes",
  "rounds": [
    {
      "type": "openquestion",
      "title": "Réponses libres",
      "answerSeconds": 30,
      "points": 1000,
      "speedBonus": 500,
      "maxLength": 40,
      "questions": [
        { "text": "Qui a peint La Joconde ?", "answer": "Léonard de Vinci", "acceptedAnswers": ["De Vinci", "Leonardo da Vinci"] },
        { "text": "De quel pays est ce drapeau ?", "answer": "L'Italie", "image": "images/drapeau.png" },
        { "text": "En quelle année l'homme a-t-il marché sur la Lune ?", "answer": "1969", "inputMode": "numeric", "answerSeconds": 15 }
      ]
    }
  ]
}
```
