### US-E15-01 — Descripteur d'une manche de blind test

**Statut :** À faire

**En tant qu'** auteur de pack
**je veux** décrire une manche de blind test dans `pack.json` : les morceaux, leur extrait, leur titre et leur artiste
**afin de** préparer un blind test sans écrire de code

**Critères d'acceptation**
- Étant donné un `pack.json`, quand une activité a le `type` `blindtest`, alors elle se compose d'un titre, des points du titre (`titlePoints`) et de l'artiste (`artistPoints`), 500 par défaut, de 0 à 10 000, et d'une liste de 1 à 50 morceaux.
- Étant donné un morceau, quand il est décrit, alors il a un extrait (`AudioExcerpt` de US-E14-02 : fichier MP3, départ, durée), un titre (1 à 100 caractères), un artiste facultatif (1 à 100 caractères) et un visuel facultatif (image), montré à la révélation.
- Étant donné une manche dont aucun morceau ne rapporte de points (`titlePoints` à 0, et `artistPoints` à 0 ou aucun artiste), quand le pack est chargé, alors le problème est signalé : la manche ne pourrait départager personne.
- Étant donné `schemas/pack.schema.json`, quand il est régénéré, alors VS Code propose et vérifie ces champs, avec leurs descriptions en français.
- Étant donné une activité `blindtest` invalide (champ manquant, borne dépassée, MP3 introuvable ou illisible, extrait au-delà de la fin du morceau), quand le pack est chargé, alors le GM voit le problème avec le fichier, le chemin dans le descripteur et sa cause, et le pack ne peut pas être lancé.
- Étant donné `packs/`, quand on consulte les exemples, alors un pack `blindtest-exemple` contient une manche de 10 extraits, avec des morceaux libres de droits dont la licence est indiquée dans le dossier du pack.

**Comportement en cas d'erreur**
Contenu invalide : détecté au chargement, partie non lançable, problème présenté au GM avant le lancement.

**Notes techniques**
- `BlindTestRoundDescriptor` et `BlindTestTrack` dans `PartyGame.Contracts.Packs`, déclarés sur `RoundDescriptor` (`[JsonDerivedType(..., "blindtest")]`).
- `BlindTestMode` dans `PartyGame.Engine/Modes/BlindTest`, enregistré dans `AddGameModes()`, vues déclarées dans `client/src/modes/registry.ts` ; en attendant US-E15-02, la manche se termine aussitôt.
- Documentation : `docs/modes/blindtest.md` (règles, phases, descripteur avec un exemple complet).

**Hors périmètre**
- Plusieurs titres ou artistes acceptés : le GM juge à l'oral.
- Un pack d'exemple avec des morceaux du commerce.
