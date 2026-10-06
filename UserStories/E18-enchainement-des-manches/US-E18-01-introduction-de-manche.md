### US-E18-01 — Écran d'introduction de chaque manche

**Statut :** Terminée

**En tant que** public
**je veux** qu'un écran annonce chaque manche avant sa première question
**afin de** savoir à quoi on joue et comment, même quand les modes changent d'une manche à l'autre

**Critères d'acceptation**
- Étant donné le lancement de la partie, ou l'entre-deux-manches, quand le GM appuie sur « Manche suivante », alors la TV affiche l'introduction de la manche : « Manche 2/4 », son titre, le nom du mode (« Blind test »), la règle du mode en deux ou trois lignes et, si le pack en fournit une, la description de la manche.
- Étant donné l'introduction, quand un téléphone l'affiche, alors il montre le numéro et le titre de la manche ainsi que la règle du mode, et aucun élément interactif.
- Étant donné l'introduction, quand la console GM l'affiche, alors elle montre la même annonce et le bouton « Commencer la manche », dont l'appui démarre la manche comme aujourd'hui (première question du mode).
- Étant donné une manche sans `description` dans le pack, quand l'introduction s'affiche, alors seule la règle du mode est montrée, sans emplacement vide.
- Étant donné une description de 300 caractères, quand la TV l'affiche en 1080p, alors elle est lisible à 3 m, sans défilement, et rien d'essentiel ne se trouve à moins de 5 % d'un bord.
- Étant donné deux consoles GM ou un double appui, quand « Commencer la manche » arrive deux fois, alors la manche ne démarre qu'une fois : l'intention nomme la manche qu'elle démarre.
- Étant donné un téléphone qui se reconnecte ou un serveur redémarré pendant l'introduction, quand l'état est restitué, alors l'introduction s'affiche de nouveau.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent la nouvelle étape dans le moteur (entrée, démarrage, rejets d'une intention obsolète ou hors étape), la validation de `description` dans le pack, les projections des trois rôles avec un test de non-fuite (aucune question de la manche dans l'introduction), et un scénario E2E qui enchaîne deux manches de modes différents.

**Comportement en cas d'erreur**
Défaut : rien côté joueurs et public. Une description trop longue est une erreur de pack, signalée au GM avant le lancement et par la commande `validate`.

**Notes techniques**
- Nouvelle phase de partie `RoundIntro` entre `Lobby` ou `BetweenRounds` et `Round`, portée par le moteur et non par les modes. `NextRound` y fait entrer ; une nouvelle intention GM `StartRound(roundId)` démarre la manche. Le mode n'est appelé qu'au démarrage.
- La phase `RoundIntro` est enregistrée comme les autres (ADR 0005) et ajoutée à la `LeakSuite` des snapshots.
- Pack : champ facultatif `description` (1 à 300 caractères) sur chaque manche, dans la base commune des descripteurs de `Contracts.Packs` ; régénérer `schemas/pack.schema.json` et mettre à jour `docs/guide-packs.md`.
- Projections : le type de mode, le numéro, le titre et la description de la manche annoncée dans les trois snapshots. Le titre de la manche n'est plus réservé au GM une fois l'introduction ouverte.
- Client : écran d'introduction dans `display/`, `player/` et `gm/` (pas dans `modes/`, comme les classements) ; textes de règle de chaque mode dans `fr.ts`, dont l'exhaustivité est vérifiée à la compilation, comme `modes/registry.ts`.
- Le bot GM de `tools/PartyGame.Bots` démarre la manche après l'introduction.
- Réalisation : moteur. `StartGame` et `NextRound` annoncent la manche (`RoundFlow.Announce`) : phase `RoundIntro`, `CurrentRound` porte déjà son `RoundId` et son index, mais pas d'état de mode (`PlayedRound.State` nul). `StartRound(roundId)` appelle `Start` du mode et passe en `Round` en gardant le même `RoundId` ; il est rejeté hors introduction (`NoRoundAnnounced`, ce qui couvre la seconde demande) ou s'il nomme une autre manche (`RoundMismatch`). Pendant l'introduction, les intentions et timers de manche sont rejetés (`NotInRound`, `UnexpectedTimer`), `SkipRound` passe la manche annoncée (une manche dont le démarrage échoue reste ainsi passable : l'incident porte la manche annoncée), `ReturnToLobby` et la reprise après redémarrage la conservent telle quelle.
- Réalisation : contrats. `Phase.RoundIntro`, `StartRoundRequest`, méthode `StartRound` du hub (`[GameMasterOnly]`). `RoundInfo` gagne `Mode` (le `type` de l'activité) et `Description`, dans toutes les phases ; en introduction, il décrit la manche annoncée, et aucune vue de mode n'est projetée, même pour le GM. `RoundDescriptor.Description` (1 à 300 caractères) est vérifiée par les contraintes communes (`PackTextLengthOutOfRange`). Le journal d'exploitation annonce « Round … started » au démarrage, pas à l'annonce.
- Réalisation : client. `selectGameScreen` rend `roundIntro` ; `display/RoundIntroScreen.svelte` (numéro, titre, nom du mode, règle, description), `player/RoundIntroScreen.svelte` (numéro, titre, règle, aucun bouton) et la branche d'introduction de `gm/RoundControl.svelte` (même annonce, « Commencer la manche »). Le nom et la règle de chaque mode sont dans `fr.modes.<type>.name` et `.rule`, dont `modes/registry.ts` vérifie l'exhaustivité à la compilation (`findModeTexts`) ; la liste des packs de la console en tire aussi le nom des modes. Le bouton entre deux manches devient « Manche suivante ». Le bandeau « Passer la manche » est aussi proposé pendant l'introduction.
- Réalisation : tests. `RoundIntroTests` (moteur), scénarios `RoundIntro` de `SnapshotsLeakTests` (manches décrites, et paire qui ne diffère que par le contenu de la manche annoncée, identique pour tous les rôles), limite de `description` dans `DescriptorProblemsTests`, tests du hub (annonce sans question, démarrage, double démarrage, message malformé), reprise après redémarrage pendant l'introduction (`TransparentResumeTests`), et `e2e/roundIntro.spec.ts` qui enchaîne une manche de quiz et une manche buzzer décrite.

**Hors périmètre**
- Un jingle d'introduction (US-E20-02).
- Une introduction de la partie entière (titre du pack sur la TV avant la première manche).
