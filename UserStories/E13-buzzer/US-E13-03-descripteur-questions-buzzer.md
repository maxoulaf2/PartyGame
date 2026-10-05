### US-E13-03 — Descripteur d'une manche de questions buzzer

**Statut :** Terminée

**En tant qu'** auteur de pack
**je veux** décrire une manche de questions buzzer dans `pack.json`, avec l'autocomplétion et une validation précise
**afin de** préparer des questions à départager au buzzer sans écrire de code

**Critères d'acceptation**
- Étant donné un `pack.json`, quand une activité a le `type` `buzzer`, alors elle se compose d'un titre, des points d'une bonne réponse (`points`, 1 000 par défaut, de 0 à 10 000) et d'une liste de 1 à 50 questions.
- Étant donné une question, quand elle est décrite, alors elle a un texte (1 à 200 caractères), une réponse attendue (1 à 100 caractères, affichée au GM puis à tous à la révélation) et une image facultative (mêmes formats que le quiz), affichée sur la TV avec la question.
- Étant donné `schemas/pack.schema.json`, quand il est régénéré (`npm run generate:contracts`), alors VS Code propose et vérifie ces champs, avec leurs descriptions en français.
- Étant donné une activité `buzzer` invalide (champ manquant, borne dépassée, image introuvable), quand le pack est chargé, alors le GM voit le problème avec le fichier, le chemin dans le descripteur et sa cause, et le pack ne peut pas être lancé.
- Étant donné le mode `buzzer` enregistré dans `AddGameModes()` et ses vues dans `client/src/modes/registry.ts`, quand une manche démarre, alors elle joue son déroulé (US-E13-04 et US-E13-05 ; en attendant, la manche se termine aussitôt, comme le quiz en US-E08-01).
- Étant donné `packs/`, quand on consulte les exemples, alors un pack `buzzer-exemple` contient une manche de 10 questions.

**Comportement en cas d'erreur**
Contenu invalide : détecté au chargement, partie non lançable, problème présenté au GM avant le lancement (US-E06-02). Rien pendant la partie.

**Notes techniques**
- `BuzzerRoundDescriptor` et `BuzzerQuestion` dans `PartyGame.Contracts.Packs`, déclarés sur `RoundDescriptor` (`[JsonDerivedType(..., "buzzer")]`).
- `BuzzerMode` dans `PartyGame.Engine/Modes/Buzzer`, avec `Validate` (cohérence au-delà des attributs : aucune pour l'instant).
- Documentation : `docs/modes/buzzer.md` (règles, phases, descripteur avec un exemple complet).

**Hors périmètre**
- Plusieurs réponses acceptées par question : le GM juge à l'oral.
- Un média audio dans une question buzzer (le blind test, E15).
