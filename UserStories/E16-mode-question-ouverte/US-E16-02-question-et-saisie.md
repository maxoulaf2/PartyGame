### US-E16-02 — Question posée et saisie sur le téléphone

**Statut :** À faire

**En tant que** joueur
**je veux** taper ma réponse sur mon téléphone, avec un clavier adapté, avant la fin du compte à rebours
**afin de** répondre librement à la question affichée sur la TV

**Critères d'acceptation**
- Étant donné une question qui commence, quand elle est présentée, alors la TV montre le titre de la manche et « Question 3/5 », la console GM le texte de la question et sa réponse attendue, et les téléphones un champ de saisie désactivé.
- Étant donné le GM qui appuie sur « Afficher la question », quand l'intention est traitée, alors la TV affiche le texte et l'image, les champs de saisie s'activent, et le compte à rebours démarre sur la TV, les téléphones et la console GM (décision 5 du README). Les joueurs inscrits à cet instant sont les participants.
- Étant donné le champ de saisie, quand il s'affiche, alors il ouvre le clavier demandé par la question (`inputMode="numeric"` ou texte), sans correction automatique ni majuscule forcée, limite la saisie à `maxLength` caractères et reste visible au-dessus du clavier sur Safari iOS et Chrome Android.
- Étant donné un joueur qui appuie sur « Envoyer » (ou sur la touche Entrée), quand sa réponse non vide part, alors elle s'affiche aussitôt « en attente », puis « Réponse enregistrée » ; le champ ne peut plus être modifié (décision 1 du README).
- Étant donné une seconde réponse du même joueur, une réponse vide après normalisation, trop longue, ou reçue après l'échéance, quand elle arrive au serveur, alors elle est rejetée sans rien changer.
- Étant donné tous les participants qui ont répondu, ou la fin du compte à rebours, quand le serveur le constate, alors les réponses se verrouillent : la TV affiche « Tous les joueurs ont répondu » ou « Temps écoulé », les téléphones sans réponse « Temps écoulé ».
- Étant donné la saisie en cours, quand les snapshots sont diffusés, alors la TV ne montre que le nombre de réponses (« 7 / 9 »), la console GM chaque réponse au fil de l'eau, et un téléphone seulement la réponse de son joueur.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent chaque transition, chaque rejet, la `LeakSuite` de chaque couple phase/rôle (paires d'états qui ne diffèrent que par la réponse attendue ou la réponse d'un autre joueur), et un scénario E2E de saisie sur iPhone (WebKit) et Pixel (Chromium).

**Comportement en cas d'erreur**
Joueur : une réponse envoyée pendant une coupure reste « en attente » et est renvoyée à la reconnexion ; elle ne compte que si les réponses sont encore ouvertes (US-E08-06). Le texte en cours de saisie, pas encore envoyé, survit à une mise en veille. Public : rien. GM : une intention obsolète est rejetée sans effet.

**Notes techniques**
- Intentions `openquestion.showQuestion` (GM, nomme la question) et `openquestion.submitAnswer` (joueur, enveloppée avec son `ClientSeq`).
- La réponse est transmise brute, le serveur ne la tronque jamais : elle est refusée si elle dépasse `maxLength`. Il la normalise pour le pré-classement (US-E16-03) mais conserve le texte saisi pour l'affichage.
- Passer une question (« Passer la question ») se comporte comme au quiz : les réponses reçues sont ignorées et personne ne marque de point.
- Persistance (ADR 0005) : les réponses reçues et l'échéance font partie de l'état de manche déclaré par le mode ; le compte à rebours repart avec le temps qui restait.

**Hors périmètre**
- Prévisualiser sur le téléphone la question ou l'image : le joueur lit la TV, comme au quiz.
