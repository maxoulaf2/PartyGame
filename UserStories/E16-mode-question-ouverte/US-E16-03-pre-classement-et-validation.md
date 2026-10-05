### US-E16-03 — Pré-classement et validation en lot par le GM

**Statut :** À faire

**En tant que** game master
**je veux** trouver les réponses déjà triées par le serveur, corriger ce qui doit l'être et valider toutes les réponses d'un coup
**afin de** juger une question ouverte en quelques secondes, sans faire attendre la salle

**Critères d'acceptation**
- Étant donné des réponses verrouillées, quand la console GM s'affiche, alors elle les regroupe : les réponses identiques après normalisation forment une ligne, avec le texte saisi le plus fréquent et les pseudos de leurs auteurs.
- Étant donné chaque groupe, quand le serveur le pré-classe, alors il est « acceptée » s'il est égal à la réponse attendue ou à une variante, « à vérifier » s'il en est proche selon la tolérance (décision 6 du README), et « refusée » sinon. Les groupes s'affichent dans cet ordre, les acceptés cochés et les autres non (décision 2 du README).
- Étant donné le GM qui coche ou décoche des groupes, quand il appuie sur « Valider », alors le jugement part en une seule intention qui nomme la question et les joueurs acceptés, et la question passe à l'état jugé.
- Étant donné aucune réponse reçue, quand les réponses se verrouillent, alors la console propose directement « Révéler ».
- Étant donné un double appui ou deux consoles ouvertes, quand le même jugement arrive deux fois, alors le second est rejeté comme obsolète. Un jugement avant le verrouillage, ou qui nomme un joueur sans réponse, est rejeté.
- Étant donné le jugement, quand les snapshots sont diffusés, alors ni la TV ni les téléphones ne voient de verdict, de catégorie ou de réponse avant la révélation (US-E16-04) ; la TV affiche « Le game master vérifie les réponses ».
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent la normalisation (casse, accents, ponctuation, espaces, articles), la tolérance selon la longueur, l'absence de tolérance en `numeric`, chaque catégorie, chaque rejet du jugement, la `LeakSuite` de la nouvelle phase, et un scénario E2E où le GM coche une réponse « à vérifier ».

**Comportement en cas d'erreur**
GM : un jugement rejeté ne change rien ; la console affiche l'état du snapshot suivant. Joueurs et public : rien.

**Notes techniques**
- Le pré-classement est calculé par le moteur au verrouillage et conservé dans l'état de manche, pour qu'une console rechargée ou un redémarrage retrouve les mêmes suggestions. Seule la projection `GameMaster` le contient.
- Distance de Levenshtein écrite dans le moteur (quelques lignes), sans dépendance : elle compare la réponse normalisée à la réponse attendue et à chaque variante, et retient la plus petite distance.
- Les cases cochées vivent dans la console jusqu'à « Valider » : deux consoles peuvent diverger, et la première validation l'emporte.

**Hors périmètre**
- Annuler un jugement après validation (E19).
- Apprendre de nouvelles variantes à partir des jugements du GM.
