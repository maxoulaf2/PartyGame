### US-E08-04 — Révélation de la bonne réponse

**Statut :** Terminée

**En tant que** public
**je veux** découvrir ensemble la bonne réponse et qui a choisi quoi
**afin de** partager le moment de la révélation et chambrer ceux qui se sont trompés

**Critères d'acceptation**
- Étant donné une question verrouillée, quand le GM appuie sur « Révéler », alors la question passe en phase `Revealed`. Le bouton n'existe pas tant que les réponses sont ouvertes : le GM verrouille d'abord (US-E08-03).
- Étant donné la révélation, quand la TV l'affiche, alors la bonne proposition est mise en évidence par une icône et un libellé en plus de la couleur, et les autres sont atténuées.
- Étant donné la révélation, quand la TV l'affiche, alors elle montre sous chaque proposition le nombre de joueurs qui l'ont choisie et leurs pseudos, puis les joueurs participants sans réponse (décision 4 du README).
- Étant donné 20 joueurs aux pseudos de 16 caractères, quand ils ont tous choisi la même proposition, alors leurs pseudos restent lisibles à 3 m sur la TV en 1080p, sans débordement ni défilement.
- Étant donné un téléphone de joueur, quand la réponse est révélée, alors il affiche « Bonne réponse ! », « Raté » avec la bonne proposition (lettre, forme et couleur, sans texte : décision 9 du README), ou « Pas de réponse ».
- Étant donné un joueur qui n'a pas participé (arrivé après l'ouverture), quand la réponse est révélée, alors son téléphone montre la bonne réponse, sans verdict.
- Étant donné la console GM, quand la réponse est révélée, alors elle affiche la même répartition que la TV et le bouton « Question suivante » (US-E08-05).
- Étant donné deux consoles GM, ou un renvoi après une reconnexion, quand deux intentions de révélation visent la même question, alors la seconde est rejetée sans effet.
- Étant donné la phase `Revealed`, quand les tests de non-fuite s'exécutent, alors la bonne réponse et les choix de tous les joueurs y sont publics : seule la phase `Revealed` les autorise.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent la révélation, son rejet hors de la phase `Locked` ou mal ciblée, et les projections des trois rôles. Un test E2E vérifie la répartition sur la TV et le verdict de chaque téléphone.

**Comportement en cas d'erreur**
Défaut : rien côté joueurs et public. Un téléphone qui se reconnecte pendant la révélation reçoit le snapshot courant, avec son verdict. GM : une révélation refusée comme obsolète n'affiche rien.

**Notes techniques**
- Intention GM `RevealAnswer(round, question)`.
- Les points gagnés s'ajoutent à l'écran du téléphone et à la console GM avec US-E09-01.
- Les pseudos sous chaque proposition suivent l'ordre d'arrivée des joueurs, comme sur le lobby, pour un affichage stable. La mise en page s'appuie sur celle de la liste du lobby (`display/playerListLayout.ts`) si elle s'y prête.
- Les pseudos sont affichés comme du texte, jamais interprétés comme du HTML (US-E04-03).
- Réalisation : contrats. Nouvelle intention GM `QuizRevealAnswer` (`quiz.revealAnswer`), qui nomme la question par son numéro (décision 10 du README). `QuizQuestionPhase` gagne `Revealed`. La vue TV ajoute `reveal` (`QuizDisplayReveal` : `correctChoice` et `answers`, des `QuizRevealedAnswer` avec joueur, pseudo et choix, pour chaque participant dans l'ordre d'arrivée), présent seulement en phase `Revealed`. La vue joueur ajoute `correctChoice`, présent seulement en phase `Revealed`, et `verdict` (`QuizVerdict` : `Correct`, `Wrong` ou `NoAnswer`), présent seulement en phase `Revealed` et pour un participant : c'est le serveur qui juge, le téléphone ne compare rien. La vue GM ne change pas : elle portait déjà la bonne réponse, le choix de chaque participant et la répartition.
- Réalisation : moteur. `QuizPhase` gagne `Revealed`. La révélation n'est acceptée qu'en phase `Locked` (sinon `PhaseMismatch`) et pour la question courante (sinon `QuestionMismatch`), sans effet. La lettre de la bonne réponse est celle de sa position dans l'ordre affiché, mélange compris.
- Réalisation : client. TV : à la révélation, l'image s'efface et la question rapetisse ; chaque proposition occupe toute la largeur, son texte à gauche, à droite le nombre de joueurs qui l'ont choisie puis leurs pseudos en étiquettes ; la bonne proposition est encadrée et marquée d'une coche et de « Bonne réponse » (composant `CorrectMark`, partagé avec la console), les autres atténuées ; les participants sans réponse suivent sous « Sans réponse ». Une question de 200 caractères, quatre propositions de 80 et 20 pseudos de 16 caractères sous la même proposition tiennent en 1080p, en 30 px au moins. Téléphone : le pavé laisse place au verdict, avec une icône (coche, croix ou tiret) et « Bonne réponse ! », « Raté » ou « Pas de réponse », puis la bonne proposition en grand (lettre, forme et couleur), précédée de « La bonne réponse » sauf si le joueur l'a choisie ; sans verdict pour un joueur arrivé après l'ouverture. Console GM : « Révéler » en phase `Locked`, puis les pseudos sous chaque proposition et « Question suivante », désactivé jusqu'à US-E08-05, à la place de « Passer la question ». Les textes `correct`, `noAnswer` et `choiceAnswers` passent de `fr.modes.quiz.gm` à `fr.modes.quiz`, partagés par les trois vues.
- Réalisation : tests. Moteur : `QuizRevealTests` (révélation après un verrouillage par le GM ou par le timer, rejets pendant la présentation, pendant les réponses, en double, d'une autre question ou d'une autre manche, réponse et verrouillage refusés après la révélation, projections des trois rôles, bonne réponse après un mélange, renommage, joueur arrivé après l'ouverture) ; `QuizLeakTests` couvre `Revealed`, avec les paires « choix de Zoé » et « Zoé a répondu ou non », cachées aux autres téléphones après la révélation. Hub : `QuizAnswersTests` révèle deux fois de suite et vérifie la répartition de la TV, le verdict de trois téléphones et ce qu'un téléphone ignore des autres. E2E : `display.spec.ts` (bonne réponse, répartition, sans réponse, et 20 pseudos de 16 caractères en 1080p), `gm.spec.ts` (« Révéler », puis répartition et « Question suivante ») et `launch.spec.ts`, qui révèle la question jouée et vérifie la TV, le verdict de chaque téléphone, le retardataire et la console.

**Hors périmètre**
- Les points et le classement (E09).
- Les animations et les sons de révélation (E20).
