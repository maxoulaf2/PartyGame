### US-E19-02 — Ajustement manuel des scores

**Statut :** Terminée

**En tant que** game master
**je veux** corriger le score d'un joueur à tout moment de la partie
**afin de** réparer une erreur de jugement, accorder un bonus ou remettre en course un joueur arrivé en retard

**Critères d'acceptation**
- Étant donné une partie lancée, quand le GM ouvre la liste des joueurs dans la console, alors chaque joueur porte son score et un bouton « Modifier le score ».
- Étant donné le formulaire, quand le GM ajoute ou retire des points (« +500 », « −200 ») ou saisit un nouveau total, alors le score du joueur change, le formulaire montre l'ancien et le nouveau total avant confirmation, et un score négatif est refusé par le formulaire comme par le serveur.
- Étant donné un ajustement confirmé, quand le snapshot suivant arrive, alors le téléphone du joueur affiche son nouveau total, et le classement de la TV et de la console en tient compte s'il est affiché, sans aucun message sur la TV.
- Étant donné un ajustement fait pendant une question, quand les points de la question sont attribués à la révélation, alors ils s'ajoutent au score ajusté.
- Étant donné deux consoles GM qui modifient le même score en même temps, quand les deux intentions arrivent, alors seule la première s'applique : chacune nomme le score qu'elle corrige, et la seconde, qui ne correspond plus, est rejetée. La console affiche alors le score à jour.
- Étant donné un retour au lobby, quand une nouvelle partie commence, alors les ajustements de la précédente ne comptent plus.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent l'ajustement dans chaque phase de la partie, ses rejets (score négatif, score attendu périmé, joueur inconnu, lobby), sa persistance, son test de non-fuite (un téléphone ne voit que son propre total) et un scénario E2E.

**Comportement en cas d'erreur**
Défaut : rien côté joueurs et public. Un ajustement rejeté laisse la console sur le score à jour, sans message technique.

**Notes techniques**
- Intention GM `AdjustScore(playerId, expectedScore, newScore)`, `[GameMasterOnly]`, traitée par le moteur hors des modes : elle modifie `Player.Score` comme l'attribution des points à la révélation.
- Les ajustements ne sont pas tracés dans l'état ; le serveur les journalise en `Information` (joueur, ancien et nouveau score).
- Le classement entre deux manches et l'évolution des rangs (US-E18-02) sont recalculés à chaque snapshot : un ajustement entre deux manches peut donc changer le classement affiché.
- Réalisation : moteur. `ScoreAdjustment` traite `AdjustScore` dans les phases `RoundIntro`, `Round`, `BetweenRounds` et `Finished`, pendant une pause comprise. Rejets : `NotAdjustable` (lobby ; `GamePending` pendant le choix d’une partie trouvée), `PlayerUnknown`, `ScoreNegative`, `ScoreObsolete` (score attendu périmé). Un nouveau total égal à l’ancien est accepté sans changement.
- Réalisation : hub. `AdjustScore` (`AdjustScoreRequest`) ne répond rien : comme `PauseGame`, les snapshots montrent le score dans tous les cas. Un ajustement accepté est journalisé en `Information`.
- Réalisation : client. Un bouton « Modifier le score » par joueur hors du lobby ouvre `gm/ScoreForm.svelte` : « Ajouter », « Retirer » ou « Nouveau total », puis des points en chiffres seuls (clavier numérique du téléphone), avec l’ancien et le nouveau total, calculés par `gm/scoreEntry.ts` depuis le score du dernier snapshot. Le formulaire se ferme une fois l’intention envoyée, acceptée ou non.
- Réalisation : tests. `ScoreAdjustmentTests` (chaque phase, pause, points de la révélation ajoutés, rejets, retour au lobby, aller-retour JSON), scénario et paire « adjusted score » de `SnapshotsLeakTests`, `AdjustScoreTests` côté hub, `scoreEntry.test.ts` et `e2e/scoreAdjustment.spec.ts`.

**Hors périmètre**
- L'historique des ajustements dans la console.
- Un ajustement en lot (plusieurs joueurs à la fois).
