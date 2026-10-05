### US-E16-04 — Révélation et points

**Statut :** À faire

**En tant que** public
**je veux** voir sur la TV la bonne réponse, puis tout ce que les joueurs ont proposé avec leurs pseudos
**afin de** savoir qui a trouvé, et de rire des réponses les plus inattendues

**Critères d'acceptation**
- Étant donné une question jugée, quand le GM appuie sur « Révéler », alors la TV affiche la réponse attendue, puis les réponses regroupées : d'abord les bonnes, puis les mauvaises, chaque groupe avec son texte et les pseudos de ses auteurs, puis les participants sans réponse (décision 4 du README).
- Étant donné de nombreuses réponses, quand la TV les affiche, alors elles tiennent à l'écran sans défilement pour 20 joueurs, avec un texte lisible de loin, et rien de collé aux bords.
- Étant donné la révélation, quand les points sont attribués, alors chaque réponse acceptée rapporte `points` plus le bonus de rapidité au prorata du temps restant à sa réception, arrondi à l'entier ; une réponse refusée ou absente rapporte 0 (décision 3 du README).
- Étant donné les téléphones, quand la question est révélée, alors chaque participant voit son verdict (« Bonne réponse ! », « Raté » ou « Pas de réponse »), la réponse attendue, les points gagnés et son nouveau total.
- Étant donné la console GM, quand la question est révélée, alors elle montre le même détail avec les points de chacun, puis « Question suivante » ou « Terminer la manche ».
- Étant donné une révélation demandée deux fois, quand la seconde arrive, alors elle est rejetée comme obsolète, et les points ne sont comptés qu'une fois.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent le barème (avec et sans bonus), la révélation sans aucune réponse, les rejets, la `LeakSuite` de la phase révélée (un téléphone ne connaît toujours que la réponse de son joueur), et le scénario E2E d'une manche complète à trois joueurs.

**Comportement en cas d'erreur**
Public : si l'image de la question ne se charge pas, la TV affiche la révélation sans elle, et la console GM signale l'incident (comme au quiz). Joueurs : rien. GM : une intention obsolète est rejetée sans effet.

**Notes techniques**
- Le classement entre deux manches et le classement final sont ceux de E09, communs à tous les modes.
- `docs/modes/openquestion.md` décrit les règles et les phases complètes ; la décision 4 du README de E08 (pseudos à la révélation) s'applique ici aussi.

**Hors périmètre**
- Masquer une réponse inconvenante avant la révélation (E19).
