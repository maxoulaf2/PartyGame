### US-E15-03 — Jugement du titre et de l'artiste, reprise après un buzz

**Statut :** Terminée

**En tant que** game master
**je veux** juger séparément le titre et l'artiste donnés par le joueur qui a la main, puis relancer la musique pour les autres
**afin que** chaque élément trouvé soit récompensé et que l'extrait se joue jusqu'au bout

**Critères d'acceptation**
- Étant donné un gagnant qui a la main, quand la console GM s'affiche, alors elle propose « Titre trouvé », « Artiste trouvé » (si le morceau a un artiste et qu'il reste à trouver) et « Rien de bon », puis « Valider » ; le jugement part en une seule intention (`blindtest.judge`, qui nomme le morceau, l'ouverture et les éléments trouvés).
- Étant donné un jugement, quand il est traité, alors chaque élément trouvé est attribué au joueur, qui ne pourra plus buzzer sur cet extrait, qu'il ait trouvé quelque chose ou non (décision 3 du README).
- Étant donné un élément encore à trouver et au moins un joueur qui peut buzzer, quand le jugement est traité, alors la musique reprend où elle s'était arrêtée (nouvel instant de déclenchement) et le buzzer se rouvre aux autres. La TV affiche ce qui a déjà été trouvé (« Titre trouvé par Léa »), sans le dévoiler.
- Étant donné plus rien à trouver, ou plus aucun joueur qui puisse buzzer, quand le jugement est traité, alors le morceau passe à la révélation (US-E15-04).
- Étant donné un double appui du GM ou deux consoles ouvertes, quand le même jugement arrive deux fois, alors le second est rejeté comme obsolète. Un élément déjà attribué ne peut pas l'être une seconde fois.
- Étant donné les téléphones, quand le jugement est traité, alors le joueur jugé voit « Bloqué pour cet extrait » et, s'il a trouvé quelque chose, ce qu'il a trouvé, sans les points tant que l'extrait n'est pas révélé.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent chaque jugement (titre, artiste, les deux, rien), chaque rejet (jugement sans gagnant, obsolète, élément déjà trouvé, artiste absent), la reprise de la musique à la bonne position, et un scénario E2E d'un buzz qui trouve le titre suivi d'un buzz qui trouve l'artiste.

**Comportement en cas d'erreur**
Joueurs et public : aucun message. GM : un jugement rejeté ne change rien ; la console affiche l'état du snapshot suivant.

**Notes techniques**
- La TV montre qui a trouvé quoi, mais jamais le titre ou l'artiste avant la révélation : la `LeakSuite` comprend des paires d'états qui ne diffèrent que par le titre ou l'artiste.
- Réouverture : nouvelle ouverture numérotée de l'arbitrage (`Reopen`, US-E13-02).

- Réalisation : intention `blindtest.judge` (GM : morceau, ouverture, `titleFound`, `artistFound`). `BlindTestRound` garde `TitleFoundBy` et `ArtistFoundBy` ; le joueur jugé est bloqué (`Buzzer.Block`). Nouveaux motifs de rejet `ElementAlreadyFound` et `ArtistMissing`. Les vues `Display` et `GameMaster` portent le pseudo de qui a trouvé chaque élément, la vue `GameMaster` l'ouverture à juger, la vue `Player` ce que le joueur a trouvé (`foundTitle`, `foundArtist`). Seuls les joueurs connectés comptent pour savoir si quelqu'un peut encore buzzer, comme pour les questions buzzer. Tests : `BlindTestJudgementTests`, scénarios et paires ajoutés à `BlindTestLeakTests`, `e2e/blindtest.spec.ts`.
- Écart : la révélation n'existant qu'avec US-E15-04, un morceau où plus rien n'est à trouver, ou que plus personne ne peut buzzer, passe dans une phase `Closed` (buzzer fermé, musique en pause) ; le GM avance avec « Passer l'extrait ». US-E15-04 y branchera la révélation.

**Hors périmètre**
- Annuler un jugement (E19).
