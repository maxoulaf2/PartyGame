### US-E10-02 — Passer une manche qui échoue à répétition

**Statut :** À faire

**En tant que** game master
**je veux** pouvoir passer une manche dont le mode plante à répétition
**afin que** un bug dans une manche ne bloque jamais le reste de la soirée

**Critères d'acceptation**
- Étant donné une manche dont trois entrées ont échoué (incident `RoundHandlerFailed`, US-E10-01), quand la console GM s'affiche, alors un bandeau bien visible propose « Cette manche rencontre un problème. Passer la manche ? ». Il reste affiché tant que la manche est en cours.
- Étant donné le bandeau, quand le GM appuie sur « Passer la manche » et confirme, alors la manche se termine sans rien demander au mode : les points déjà ajoutés aux scores restent, ceux de la question en cours ne sont pas attribués, les timers de la manche sont annulés. La partie passe entre deux manches, avec le classement intermédiaire, ou se termine avec le classement final si c'était la dernière.
- Étant donné une manche passée, quand la TV et les téléphones affichent la suite, alors ils montrent l'écran habituel de fin de manche, sans aucun message au sujet de l'incident.
- Étant donné une manche passée, quand la manche suivante démarre, alors elle se joue normalement : le compteur d'échecs repart de zéro pour chaque manche.
- Étant donné deux consoles GM, ou un double appui, quand l'intention arrive deux fois, alors une seule manche est passée : l'intention nomme la manche qu'elle passe, et le moteur rejette comme obsolète celle qui ne correspond plus à la manche courante.
- Étant donné l'intention envoyée sans le code GM, quand le hub la reçoit, alors elle est refusée.
- Étant donné les tests du moteur, quand ils s'exécutent, alors ils couvrent : manche passée en milieu de manche, dernière manche passée, points conservés, timers annulés, intention obsolète, intention hors manche (lobby, entre deux manches, partie terminée).

**Comportement en cas d'erreur**
Défaut. Si passer la manche échouait elle-même, ce serait un bug du moteur : état conservé, incident GM, et le bandeau reste proposé.

**Notes techniques**
- Nouvelle intention GM générique `SkipRound(roundId)`, portée par le moteur et non par un mode (décision 1 du README) : une méthode `[GameMasterOnly]` du hub, comme `NextRound`. Le moteur l'accepte à tout moment d'une manche ; seule la console limite son affichage aux manches défaillantes. E19 l'exposera en permanence (saut de manche).
- Le seuil de 3 échecs et le compteur par manche vivent dans le journal des incidents (US-E10-01), hors de l'état. Le journal indique la manche à proposer dans la liste envoyée au GM.
- La fin de manche réutilise le chemin de fin normal de US-E07-01, sans appeler le mode, puisque c'est lui qui échoue.
- Projections : la manche passée est mémorisée comme terminée. Seule la projection `GameMaster` peut indiquer qu'elle a été passée ; la `LeakSuite` du moteur le vérifie pour `Player` et `Display`.

**Hors périmètre**
- Le saut de manche permanent et le réordonnancement (E19).
- La reprise d'une manche passée.
