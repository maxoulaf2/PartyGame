# E07 — Modèle des modes de jeu

**Phase :** 2. Premier mode : quiz QCM
**Objectif :** la partie enchaîne les manches du pack choisi, chacune jouée par le mode que désigne son `type`. Un mode s'ajoute en implémentant `IGameMode`, ses vues client et ses types de contrat, puis en l'enregistrant, sans toucher au moteur, à la boucle, au hub ni aux pages. Un outil de test commun garantit qu'aucun mode ne laisse fuiter une information secrète.

## User stories

| US | Titre | Statut | Dépend de |
|---|---|---|---|
| [US-E07-01](US-E07-01-enchainement-des-manches.md) | Manches du pack enchaînées par le moteur | Terminée | — |
| [US-E07-02](US-E07-02-vues-des-modes.md) | Vues de chaque mode côté client | À faire | US-E07-01 |
| [US-E07-03](US-E07-03-non-fuite.md) | Aucune fuite d'information, quel que soit le mode | À faire | US-E07-01 |

## Décisions

Toutes les décisions qui bloquaient l'épopée sont prises.

1. **Idempotence des intentions GM par l'étape visée :** une intention GM qui fait avancer la partie (manche suivante, ouverture des réponses, révélation, question suivante…) nomme l'étape qu'elle fait avancer : manche, question et phase. Un renvoi après une reconnexion, un double appui ou deux consoles GM qui agissent en même temps sont rejetés comme obsolètes, et une étape n'est jamais sautée par erreur. Les intentions des joueurs gardent le `ClientSeq` des conventions (US-E08-06). Option écartée : un `ClientSeq` par console GM, qui laisserait deux consoles faire avancer la partie deux fois.
2. **Pas d'écran d'introduction de manche en phase 2 :** la première question d'une manche s'affiche directement, avec le titre de la manche. L'écran d'introduction arrive avec l'enchaînement des manches de E18.
3. **Types `quiz` provisoires pour les bases polymorphes des manches** (décidé pendant US-E07-01) : System.Text.Json et `PartyGame.TypeGen` refusent une base `[JsonPolymorphic]` sans type dérivé. Les vues (`PlayerRoundView`, `DisplayRoundView`, `GameMasterRoundView`) et les intentions de manche (`PlayerRoundIntent`, `GameMasterRoundIntent`) déclarent donc dès maintenant des types `quiz` vides, comme `QuizRoundDescriptor` en US-E06-01. Le hub et le moteur sont complets dès E07, et E08 n'ajoute que ses types et leurs `[JsonDerivedType]`. Options écartées : reporter les intentions et leurs méthodes du hub à E08, qui aurait alors modifié `GameHub` ; enregistrer les types des modes à l'exécution par un modificateur de `TypeInfoResolver`, plus complexe et qui aurait exigé une union vide (`never`) côté TypeScript.

## Ordre de réalisation suggéré

1. US-E07-01, en parallèle de US-E06-01.
2. US-E07-02 et US-E07-03, avant les vues et les projections du quiz (US-E08-02 et suivantes).

## Critère de sortie (phase 2)

Porté par E09 : une partie de 10 questions jouée de bout en bout par trois joueurs, et les tests de non-fuite qui passent pour chaque phase et chaque rôle.
