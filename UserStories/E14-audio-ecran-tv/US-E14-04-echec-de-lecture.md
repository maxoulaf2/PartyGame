### US-E14-04 — Échec de lecture d'un média audio

**Statut :** Terminée

**En tant que** game master
**je veux** être prévenu quand la TV ne peut pas lire un extrait, sans que le public voie quoi que ce soit
**afin de** passer l'étape et continuer la soirée

**Critères d'acceptation**
- Étant donné un MP3 que la TV ne parvient pas à charger ou à décoder, quand l'erreur survient, alors la TV le signale au serveur (`ReportDisplayMediaFailure`) et garde son affichage courant, sans message ni écran vide.
- Étant donné ce signalement, quand le serveur le reçoit, alors la console GM affiche l'incident `DisplayMediaFailed`, avec la manche et l'étape que donne `IGameMode.LocateMedia`, comme pour une image (US-E10-04).
- Étant donné un média qui n'a pas commencé à jouer 3 s après l'instant de déclenchement, quand ce délai expire, alors la TV le signale de la même façon.
- Étant donné le même média qui échoue plusieurs fois, quand la TV le signale, alors un seul incident est affiché par étape.
- Étant donné les tests, quand ils s'exécutent, alors Playwright vérifie, avec un média servi en erreur, que la TV garde son affichage et que la console GM reçoit l'incident.

**Comportement en cas d'erreur**
Joueurs et public : rien. GM : l'incident, et la possibilité de passer l'étape avec les contrôles du mode (« Passer l'extrait » en E15) ou de passer la manche (US-E10-02).

**Notes techniques**
- Réutilise le signalement des médias de US-E10-04 : pas de nouveau contrat.
- Un échec de lecture ne change pas l'état de jeu : le moteur n'en sait rien.

- Réalisation : `ExcerptPlayer` signale le fichier sur l'événement `error` de l'élément `<audio>`, ou quand l'élément n'a pas émis `playing` 3 s (`startTimeout`) après lui avoir demandé de jouer ; un écran dont l'audio est verrouillé n'est pas signalé, la console prévient déjà. Un fichier n'est signalé qu'une fois tant qu'il reste le même ; le serveur regroupe déjà les incidents par étape (US-E10-04). Tests : `excerptPlayer.test.ts`, `e2e/blindtest.spec.ts`.

**Hors périmètre**
- Une nouvelle tentative automatique de lecture.
