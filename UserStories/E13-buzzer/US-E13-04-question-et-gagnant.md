### US-E13-04 — Question posée et annonce du gagnant

**Statut :** À faire

**En tant que** game master
**je veux** poser une question qui ouvre le buzzer, et voir qui a la main
**afin de** donner la parole au plus rapide sans contestation possible

**Critères d'acceptation**
- Étant donné une manche de questions buzzer, quand une question commence, alors la TV affiche « Question n / N », les téléphones un buzzer fermé, et la console GM le texte de la question, sa réponse attendue et le bouton « Poser la question ».
- Étant donné le GM qui pose la question (`buzzer.askQuestion`, qui nomme la question), quand l'intention est traitée, alors la TV affiche la question et son image, et le buzzer s'ouvre sur tous les téléphones des joueurs connectés. Un second envoi est rejeté comme obsolète.
- Étant donné un buzzer ouvert, quand l'arbitrage désigne un gagnant (US-E13-02), alors la TV affiche son pseudo en grand, son téléphone affiche « À toi ! », et les autres téléphones affichent le pseudo du gagnant, buzzer fermé.
- Étant donné la console GM, quand un gagnant est désigné, alors elle affiche son pseudo et les boutons « Bonne réponse » et « Mauvaise réponse » (US-E13-05).
- Étant donné un joueur arrivé pendant la question, quand le buzzer s'ouvre ou se rouvre, alors il peut buzzer. Comme pour le quiz, il commence à 0 point (décision 4 du README de E09).
- Étant donné un téléphone qui se reconnecte, la TV qui se recharge ou une partie reprise après un crash, quand le snapshot arrive, alors chacun retrouve l'état courant : buzzer ouvert, gagnant désigné ou joueur bloqué.
- Étant donné les projections, quand elles sont produites, alors seule celle du GM contient la réponse attendue avant la révélation, et aucune ne contient les horodatages des buzz : la `LeakSuite` du mode couvre chaque phase et chaque rôle.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent chaque transition et chaque rejet du moteur, la non-fuite, et un scénario E2E avec trois joueurs, la TV et le GM.

**Comportement en cas d'erreur**
Joueurs et public : aucun message ; une image introuvable laisse la question affichée sans image, avec l'incident `DisplayMediaFailed` côté GM (US-E10-04). GM : un échec du mode suit US-E10-02.

**Notes techniques**
- Phases : `Ready` (question annoncée), `Open` (buzzer ouvert), `Arbitrating` (fenêtre en cours, buzzer encore ouvert pour les téléphones), `Answering` (un gagnant a la main), `Revealed`.
- Vues dans `client/src/modes/buzzer/` : `BuzzerPlayer.svelte` (avec `BuzzerButton` de US-E13-01), `BuzzerDisplay.svelte`, `BuzzerGameMaster.svelte`. Textes dans `fr.ts`.
- `ResumeRound` : décale l'ouverture et les buzz retenus, reprogramme le timer d'arbitrage.
- `LocateMedia` et `StepOf` renvoient le numéro de la question.

**Hors périmètre**
- Un son de buzz sur la TV (habillage, E20).
- Un compte à rebours de réponse pour le gagnant.
