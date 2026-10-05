### US-E15-02 — Écoute de l'extrait et buzz

**Statut :** À faire

**En tant que** joueur
**je veux** buzzer pendant que l'extrait joue sur la TV, et que la musique s'arrête dès qu'un joueur a la main
**afin de** répondre avant les autres, dans le silence

**Critères d'acceptation**
- Étant donné une manche de blind test, quand un morceau commence, alors la TV affiche « Extrait n / N » et précharge l'extrait, les téléphones un buzzer fermé, et la console GM le titre, l'artiste et le bouton « Lancer l'extrait ».
- Étant donné le GM qui lance l'extrait (`blindtest.play`, qui nomme le morceau), quand l'intention est traitée, alors la TV joue l'extrait à l'instant fixé par le serveur (US-E14-03) et le buzzer s'ouvre sur les téléphones au même instant. Un second envoi est rejeté comme obsolète.
- Étant donné un extrait qui joue, quand l'arbitrage désigne un gagnant (US-E13-02), alors la musique s'arrête, la TV affiche son pseudo en grand, son téléphone « À toi ! », les autres téléphones le pseudo du gagnant, et la console GM les boutons de jugement (US-E15-03).
- Étant donné un extrait arrivé à sa fin sans buzz retenu, quand la TV atteint la durée, alors la musique s'arrête et le buzzer reste ouvert (décision 5 du README) ; la console GM propose « Révéler la réponse ».
- Étant donné les projections, quand elles sont produites, alors seule celle du GM contient le titre, l'artiste et le visuel avant la révélation ; la projection `Display` ne contient que l'identifiant opaque de l'extrait, servi sans étiquettes ID3 (US-E14-02). La `LeakSuite` du mode couvre chaque phase et chaque rôle.
- Étant donné un téléphone qui se reconnecte, la TV qui se recharge ou une partie reprise après un crash, quand le snapshot arrive, alors chacun retrouve l'état courant, et la TV reprend la musique là où elle en est si elle joue.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent les transitions et les rejets du moteur, la non-fuite, et un scénario E2E avec trois joueurs, la TV et le GM, dont la pause au buzz.

**Comportement en cas d'erreur**
Joueurs et public : aucun message. GM : un extrait illisible produit l'incident `DisplayMediaFailed` (US-E14-04) ; « Passer l'extrait » (`blindtest.skipTrack`) passe au morceau suivant sans points.

**Notes techniques**
- Phases : `Ready`, `Listening` (extrait en cours, buzzer ouvert), `Arbitrating`, `Answering` (musique en pause), `Revealed`.
- L'état de la manche garde la position atteinte dans l'extrait, pour la pause et la reprise (décision 4 du README) ; la vue `Display` porte l'`AudioPlayback` de US-E14-03.
- Vues dans `client/src/modes/blindtest/`, avec `BuzzerButton` (US-E13-01) et `shared/audio` (US-E14-03).
- `ResumeRound`, `LocateMedia` (extrait et visuel) et `StepOf` renvoient le numéro du morceau.

**Hors périmètre**
- Le jugement et la reprise de la musique (US-E15-03).
