# Mode question ouverte

**Type d'activité :** `openquestion`
**Épopée :** [E16](../../UserStories/E16-mode-question-ouverte/README.md)

Le game master affiche une question sur l'écran TV. Chaque joueur tape sa réponse sur son téléphone avant la fin d'un compte à rebours. Le serveur pré-classe les réponses, le GM valide le lot d'un coup d'œil, puis la TV révèle la bonne réponse avec tout ce que les joueurs ont proposé.

> **État de la réalisation :** le descripteur et ses vérifications sont en place (US-E16-01), ainsi que la question posée, la saisie sur le téléphone et le verrouillage des réponses (US-E16-02), le pré-classement et la validation par le GM (US-E16-03). La révélation et les points (US-E16-04) restent à faire : en attendant, une fois les réponses jugées, le GM passe à la suite en passant la question, sans que personne ne marque de point.

## Règles

- **Compte à rebours et réponse définitive.** Chaque question a une durée de réponse (`answerSeconds`). Un joueur n'envoie qu'une réponse, qu'il ne peut plus corriger.
- **Comparaison tolérante.** Avant d'être comparée à la réponse attendue et à ses variantes, une réponse est normalisée : minuscules, sans accents, ponctuation et symboles remplacés par des espaces, espaces superflus retirés, et article initial ôté (le, la, les, l', un, une, des, the). « L'Italie ! » et « italie » sont donc la même réponse. Les fautes de frappe sont tolérées selon la longueur de la réponse attendue normalisée (distance de Levenshtein à la réponse attendue ou à la variante la plus proche) : aucune jusqu'à 3 caractères, 1 de 4 à 7, 2 au-delà. Une question numérique n'a aucune tolérance. Une réponse tolérée n'est qu'une suggestion « À vérifier » : c'est le GM qui tranche.
- **Points.** Une réponse acceptée rapporte les points de la manche (`points`), plus un bonus de rapidité facultatif (`speedBonus`) en proportion du temps restant.

## Phases d'une question

| Phase | Ce qui se passe | Passage à la suivante |
|---|---|---|
| `Presentation` | La TV montre le titre de la manche, le numéro de la question (« Question 3/5 ») et, en grand, « Question 3 ». La console GM affiche le texte de la question, avec la mention « Pas encore affichée sur la TV », sa réponse attendue et ses variantes. Les téléphones montrent le champ de saisie, désactivé. | Le GM lit la question puis appuie sur « Afficher la question ». |
| `Answering` | La TV affiche le texte et l'image de la question. Les joueurs inscrits à cet instant participent : leur champ de saisie s'active, avec le clavier demandé par la question (texte ou chiffres), sans correction automatique ni majuscule forcée, limité à `maxLength` caractères. Le compte à rebours tourne sur la TV, les téléphones et la console GM. Un joueur envoie sa réponse avec « Envoyer » ou la touche Entrée : elle s'affiche aussitôt « en attente », puis « Réponse enregistrée », et ne peut plus être modifiée. La TV ne montre que le nombre de réponses (« Réponses : 7 / 9 »), la console GM chaque réponse au fil de l'eau, et un téléphone seulement celle de son joueur. Un joueur arrivé après l'ouverture jouera à la question suivante. Le texte tapé et pas encore envoyé survit à une mise en veille ou à un rechargement ; une réponse envoyée pendant une coupure reste « en attente » et est renvoyée au retour de la connexion, et elle ne compte que si les réponses sont encore ouvertes. | Réponse du dernier participant, ou fin du compte à rebours. Une seconde réponse, une réponse vide une fois normalisée, plus longue que `maxLength` (le serveur ne la tronque jamais) ou reçue après l'échéance est refusée. |
| `Locked` | Plus aucune réponse n'est acceptée. La TV affiche « Tous les joueurs ont répondu » ou « Temps écoulé », puis « Le game master vérifie les réponses ». Un téléphone dont le joueur a répondu garde sa réponse et « Réponse enregistrée », les autres affichent « Temps écoulé ». La console GM regroupe les réponses identiques une fois normalisées (une ligne par groupe, avec le texte le plus souvent saisi et les pseudos de ses auteurs) et les pré-classe : « Acceptée » (égale à la réponse attendue ou à une variante), « À vérifier » (proche, voir la tolérance ci-dessous) ou « Refusée ». Les groupes s'affichent dans cet ordre, les acceptés cochés. Le GM coche ou décoche ce qu'il veut, puis appuie sur « Valider ». | « Valider » envoie le jugement de tout le lot en une fois. Sans aucune réponse reçue, la question passe directement à `Judged`. |
| `Judged` | La console GM montre le verdict de chaque groupe (« Bonne réponse » ou « Mauvaise réponse »). La TV et les téléphones ne voient ni verdict, ni catégorie, ni réponse : rien ne change pour eux. Un second jugement (double appui, seconde console) est rejeté : le premier l'emporte. | Pour l'instant, « Passer la question » (US-E16-04 ajoutera la révélation). |

Le GM peut passer la question dans chaque phase, après confirmation : les réponses reçues sont ignorées et personne ne marque de point, le compte à rebours s'arrête, et la question suivante est présentée, ou la manche se termine si c'était la dernière.

Après un redémarrage du serveur, une partie reprise retrouve la question dans sa phase, avec les réponses déjà reçues ; un compte à rebours en cours repart avec le temps qui lui restait.

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
