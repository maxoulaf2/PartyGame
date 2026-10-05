### US-E15-04 — Révélation et points

**Statut :** Terminée

**En tant que** public
**je veux** découvrir le titre, l'artiste et le visuel du morceau, et qui les a trouvés
**afin de** clore chaque extrait sur un moment partagé

**Critères d'acceptation**
- Étant donné un morceau dont tout a été trouvé ou que personne ne peut plus buzzer, ou le GM qui choisit « Révéler la réponse » à tout moment, quand la révélation a lieu, alors la musique s'arrête et la TV affiche le titre, l'artiste, le visuel s'il existe, et le pseudo de qui a trouvé chaque élément.
- Étant donné la révélation, quand elle a lieu, alors les points de chaque élément trouvé s'ajoutent aux scores (décision 3 du README de E09) et chaque téléphone affiche les points gagnés par son joueur sur l'extrait.
- Étant donné la révélation, quand le GM passe au morceau suivant (`blindtest.nextTrack`), alors le morceau suivant commence (US-E15-02) ; après le dernier, la manche se termine et le classement intermédiaire s'affiche (US-E09-02).
- Étant donné un morceau passé (`blindtest.skipTrack`) avant sa révélation, quand il est passé, alors il ne rapporte aucun point, comme une question de quiz passée.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent le calcul des points (titre seul, artiste seul, les deux par deux joueurs, morceau sans artiste, morceau passé), la non-fuite jusqu'à la révélation, et le scénario E2E de bout en bout d'une manche de deux extraits.

**Comportement en cas d'erreur**
Joueurs et public : un visuel introuvable laisse la révélation sans visuel, avec l'incident `DisplayMediaFailed` côté GM.

**Notes techniques**
- Mettre à jour `docs/modes/blindtest.md`.

- Réalisation : intentions `blindtest.revealAnswer` et `blindtest.nextTrack` (GM : morceau). La phase `Closed` de US-E15-03 devient `Revealed`, toujours déduite du buzzer fermé : aucun champ persisté de plus. La révélation arrête la musique (lecture en pause), ferme le buzzer et attribue les points en une transition ; un morceau passé n'en rapporte aucun. La vue `Display` porte le titre, l'artiste et l'URL du visuel une fois révélés, la vue `Player` les points gagnés sur l'extrait (`points`). « Passer l'extrait » n'est plus proposé une fois le morceau révélé. Tests : `BlindTestRevealTests`, scénarios et paires ajoutés à `BlindTestLeakTests` (le titre, l'artiste et le visuel restent cachés aux téléphones après la révélation), `e2e/blindtest.spec.ts`.
- Écart : le pack E2E n'a qu'une manche, donc le dernier extrait mène au classement final plutôt qu'au classement intermédiaire, qui relève du moteur commun (US-E09-02).

**Hors périmètre**
- La lecture du morceau en entier à la révélation (habillage, E20).
