# Mode questions buzzer

**Type d'activité :** `buzzer`
**Épopée :** [E13](../../UserStories/E13-buzzer/README.md)

Le game master pose une question, qui s'affiche sur l'écran TV pendant que le buzzer s'ouvre sur les téléphones. Le premier joueur qui buzze répond à voix haute, et le GM juge sa réponse depuis sa console. La question se joue jusqu'à ce que quelqu'un trouve, ou que le GM révèle la réponse.

## Règles

- **Question affichée à l'ouverture du buzzer.** Le GM pose la question : la TV l'affiche, avec son image s'il y en a une, et le buzzer s'ouvre sur tous les téléphones. Le GM peut aussi la lire à voix haute. La réponse attendue n'apparaît que sur la console GM, jusqu'à la révélation.
- **Départage à l'horodatage.** Le gagnant est celui dont le doigt a touché l'écran en premier, d'après l'heure serveur de l'appui, et non d'après l'arrivée de son message. Le serveur attend une fenêtre d'arbitrage après le premier buzz reçu (250 ms par défaut, `Buzzer:ArbitrationMilliseconds`).
- **Réponse orale, jugée par le GM.** Le joueur qui a la main répond à voix haute. Le téléphone ne sert qu'à buzzer.
- **Mauvaise réponse.** Le joueur est bloqué pour la question, sans perdre de points, et le buzzer se rouvre aussitôt aux autres. Quand tous les joueurs connectés sont bloqués, le buzzer reste fermé et le GM ne peut plus que révéler la réponse.
- **Bonne réponse.** Elle rapporte les points de la manche (`points`, 1 000 par défaut) et révèle aussitôt la réponse.
- **Révélation sans points.** À tout moment une fois la question posée, le GM peut révéler la réponse : personne ne marque.
- **Images.** Une question peut avoir une image, affichée sur l'écran TV avec la question. Les téléphones n'affichent aucun média.

## Phases d'une question

| Phase | TV | Téléphones | Console GM |
|---|---|---|---|
| `Ready` : question annoncée | « Question n » | Buzzer fermé | Texte, réponse attendue, bouton « Poser la question » |
| `Open` : buzzer ouvert (`buzzer.askQuestion`) | Question et image | Buzzer ouvert | « Buzzer ouvert : en attente d'un buzz » |
| `Arbitrating` : fenêtre d'arbitrage en cours | Comme `Open` | Comme `Open` ; « Buzz envoyé… » pour qui a buzzé | Comme `Open` |
| `Answering` : un gagnant a la main | Question et « Pseudo a la main » | « À toi de répondre ! » pour le gagnant, son pseudo pour les autres | « Pseudo a la main », « Bonne réponse », « Mauvaise réponse », « Révéler la réponse » |
| `Closed` : tous les joueurs connectés sont bloqués | Question | Buzzer bloqué | « Tous les joueurs sont bloqués », « Révéler la réponse » |
| `Revealed` : réponse révélée | Question, réponse et « Pseudo a trouvé ! » (ou « Personne n'a trouvé ») | Points gagnés sur la question et total | « Question suivante », ou « Terminer la manche » après la dernière |

La fenêtre d'arbitrage reste invisible : les projections montrent `Open` tant que le gagnant n'est pas désigné, et aucune ne contient les horodatages des buzz. Un buzz (`buzzer.buzz`) nomme la question et l'ouverture du buzzer : renvoyé après une reconnexion, il ne compte jamais pour une ouverture suivante. Un joueur arrivé pendant la question peut buzzer.

Les intentions du GM nomment la question : `buzzer.askQuestion`, `buzzer.revealAnswer` et `buzzer.nextQuestion`. Le jugement, `buzzer.judge`, nomme en plus l'ouverture du buzzer qu'il juge : envoyé deux fois, ou par deux consoles, il est rejeté une fois le buzzer rouvert, si bien qu'un joueur ne gagne jamais deux fois et qu'un refus ne bloque jamais le gagnant suivant. Après la dernière question, `buzzer.nextQuestion` termine la manche et le classement intermédiaire s'affiche.

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
