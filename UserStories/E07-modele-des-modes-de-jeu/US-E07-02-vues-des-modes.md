### US-E07-02 — Vues de chaque mode côté client

**Statut :** Terminée

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
- Réalisation : le choix de l'écran est une fonction pure, `selectGameScreen` (`shared/gameScreen.ts`), commune aux trois pages : `lobby`, `round` (vue de manche et composant du mode), `betweenRounds`, `finished`, ou `waiting`, l'écran d'attente neutre, pour un mode inconnu, une manche sans vue ou une phase inconnue (serveur plus récent).
- Réalisation : le registre (`modes/registry.ts`) associe chaque `type` à ses trois composants, avec `satisfies { [T in RoundViewType]: ModeViews<T> }` : un `type` des contrats absent du registre, un `type` inconnu des contrats, ou un composant dont les props ne prennent pas les vues de son `type`, font échouer `svelte-check`. `findPlayerView`, `findDisplayView` et `findGameMasterView` ne lisent que les clés propres du registre : `toString` ou `__proto__` sont des modes inconnus. Les props des vues (`PlayerViewProps`, `DisplayViewProps`, `GameMasterViewProps`) sont dans `shared/modeViews.ts`, pour qu'un mode les importe sans sortir de `shared/`. La vue TV ne reçoit ni `interactive` ni envoi : l'écran TV n'agit jamais.
- Réalisation : chaque vue de mode est rendue dans un `{#key}` sur l'identifiant de la manche : une nouvelle manche repart d'un état local vierge.
- Réalisation : envoi des intentions. `PlayerSession.sendRoundIntent` (`SendRoundIntent`, seulement une fois le joueur reconnu), `GameMasterSession.sendRoundIntent` (`SendGameMasterRoundIntent`) et `GameMasterSession.nextRound(afterRound)` (`NextRound`) répondent `sent` ou `unreachable` (`IntentOutcome`) : le hub ne répond rien, et l'acceptation se lit dans le snapshot suivant.
- Réalisation : écrans provisoires. Téléphone : « Fin de la manche 1/2 » puis « Partie terminée », sur l'écran d'attente commun. TV : hors manche, le lobby reste affiché (QR code et liste des joueurs, pour les retardataires) avec un titre « Fin de la manche 1/2 », « Partie terminée » ou, pour un mode inconnu, « Partie en cours ». Console GM : renommée `GameConsole.svelte`, elle garde joueurs, adresse et pack pendant toute la partie, et `RoundControl.svelte` y affiche la manche (numéro, titre, vue du mode), le bouton « Lancer la manche suivante » entre deux manches, ou « Partie terminée ». Les textes communs sont sous `fr.game`, ceux des modes sous `fr.modes.<type>` ; `fill` et `roundText` (`shared/i18n/fill.ts`) remplacent les copies locales.
- Réalisation : vues provisoires du quiz dans `modes/quiz/` (`QuizPlayer`, `QuizDisplay`, `QuizGameMaster`) : le numéro et le titre de la manche, en attendant ses questions (US-E08-02). Tant que la manche de quiz se termine dès son démarrage (décision 8 de E08), l'application réelle passe directement du lancement à « Fin de la manche ».
- Réalisation : la règle maison `partygame/mode-boundaries` (`client/eslint/modeBoundaries.js`) résout chaque import relatif d'un fichier de `src/modes/<mode>/`, à toute profondeur, et refuse ce qui sort de `src/shared/` et du dossier du mode (autre mode, pages, registre) ; `@microsoft/signalr` reste refusé par `no-restricted-imports`. Aucune dépendance ajoutée.
- Réalisation : tests. Vitest : `gameScreen.test.ts` (type connu, type inconnu, manche sans vue, entre deux manches, partie terminée, phase inconnue), `registry.test.ts`, `fill.test.ts`, les nouveaux envois dans `playerSession.test.ts` et `gameMasterSession.test.ts`, et `eslint/modeBoundaries.test.ts`, qui lance ESLint avec la configuration du projet. E2E : `launch.spec.ts` suit la vraie partie du lancement à la fin (entre deux manches, retardataire, manche suivante, partie terminée sur les trois interfaces) ; `display.spec.ts` et `gm.spec.ts` montrent une manche de quiz et l'écran neutre d'un mode inconnu ; la mise en page TV est vérifiée entre deux manches et en fin de partie. `joinOnNewPhone` attend la disparition du formulaire plutôt que l'écran du lobby.

**Hors périmètre**
- Les vues du quiz (E08).
- Le `<svelte:boundary>` et les handlers globaux d'erreurs (E10).
- Les transitions et animations entre écrans (E20).
