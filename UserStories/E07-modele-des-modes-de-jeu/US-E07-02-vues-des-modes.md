### US-E07-02 — Vues de chaque mode côté client

**Statut :** À faire

**En tant que** joueur
**je veux** que mon téléphone affiche l'écran de la manche en cours, quel que soit son mode
**afin de** jouer toute la soirée sans recharger la page ni en changer

**Critères d'acceptation**
- Étant donné un mode, quand ses vues sont écrites, alors elles vivent dans `client/src/modes/<mode>/` : une vue `player`, une vue `display` et une vue `gm`.
- Étant donné le registre des modes du client, quand un mode y est déclaré, alors il associe le `type` de ses vues de manche à ses trois composants. Le registre est explicite et vérifié à la compilation : un `type` de vue présent dans les contrats générés mais absent du registre fait échouer `npm run check`.
- Étant donné une manche qui démarre, quand le snapshot arrive, alors chaque page (joueur, TV, GM) affiche la vue de son rôle pour le mode de la manche, sans rechargement. Elle lui transmet la vue de manche du snapshot, l'état `interactive` (US-E05-02) et l'envoi des intentions de manche.
- Étant donné la partie entre deux manches ou terminée, quand le snapshot arrive, alors chaque page affiche un écran d'attente provisoire (« Fin de la manche », « Partie terminée »), remplacé par les classements de E09.
- Étant donné un `type` de vue inconnu du client, quand il est reçu, alors la page affiche l'écran d'attente neutre, sans aucun message technique.
- Étant donné une vue de mode, quand ESLint s'exécute, alors il refuse qu'elle importe un autre mode, une page (`player/`, `display/`, `gm/`) ou `@microsoft/signalr` : un mode ne dépend que de `shared/` et de son propre dossier.
- Étant donné les textes d'un mode, quand ils sont écrits, alors ils sont dans `fr.ts`, sous une clé propre au mode.
- Étant donné les tests Vitest, quand ils s'exécutent, alors ils couvrent le choix de la vue (type connu, type inconnu, entre deux manches, partie terminée).

**Comportement en cas d'erreur**
Joueurs et public : jamais d'écran vide ni de message technique, l'écran d'attente neutre remplace une vue impossible à afficher. Le `<svelte:boundary>` de chaque vue et la remontée des erreurs au serveur sont l'objet de E10. GM : idem.

**Notes techniques**
- Registre dans `client/src/modes/registry.ts`, typé par l'union générée des vues de manche (par exemple avec `satisfies Record<PlayerRoundView['type'], …>`), pour l'exhaustivité.
- L'envoi des intentions passe par `shared/connection`, qui expose une fonction typée par l'union des intentions de manche. Les vues n'en savent pas plus.
- Les composants communs aux modes (compte à rebours, propositions…) vont dans `shared/components/` dès qu'un second mode en a besoin, pas avant.
- Règle ESLint `no-restricted-imports` (ou `import/no-restricted-paths`, si la dépendance existe déjà) limitée à `client/src/modes/**`.

**Hors périmètre**
- Les vues du quiz (E08).
- Le `<svelte:boundary>` et les handlers globaux d'erreurs (E10).
- Les transitions et animations entre écrans (E20).
