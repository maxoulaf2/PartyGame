### US-E01-06 — ADR 0001 : architecture générale

**Statut :** Terminée

**Résultat attendu**
Les choix d'architecture déjà retenus sont consignés dans `docs/adr/0001-architecture-generale.md`, au format Contexte / Décision / Conséquences, pour qu'un nouveau contributeur comprenne pourquoi le serveur est construit ainsi.

**Critères d'acceptation**
- Étant donné le dossier `docs/adr/`, quand on l'ouvre, alors il contient `0001-architecture-generale.md` et un gabarit `0000-modele.md` pour les ADR suivants.
- Étant donné l'ADR 0001, quand on le lit, alors il couvre au minimum : le serveur autoritaire, la boucle unique par partie alimentée par un `Channel<GameInput>`, le moteur pur (`Handle(state, input, context)` → `Transition`) avec état immuable et effets, les snapshots complets versionnés et projetés par rôle, et l'hébergement sur PC avec le Raspberry Pi en cible secondaire.
- Étant donné la section Contexte, quand on la lit, alors elle explique les contraintes qui motivent ces choix : appels concurrents du hub SignalR, réseau local instable, téléphones mis en veille, absence de fuite d'information.
- Étant donné la section Conséquences, quand on la lit, alors elle cite les bénéfices (pas de verrous, tests déterministes, retour arrière gratuit, persistance simple) et les coûts (snapshots complets plus lourds que des diffs, tout passe par la file).
- Étant donné le tableau « Décisions » de CLAUDE.md, quand on le lit, alors les lignes concernées renvoient vers l'ADR 0001.

**Comportement en cas d'erreur**
Sans objet.

**Notes techniques**
- Documentation en français, conformément aux conventions.
- Les alternatives écartées (verrous, état mutable, diffs) sont mentionnées avec la raison de leur rejet.
- Aucune dépendance : peut être réalisée en parallèle de US-E01-01.

**Hors périmètre**
- ADR sur le choix du front et sur l'outil de génération des types : ils sont rédigés lors de la décision correspondante, au début de US-E01-02 et US-E01-04.
