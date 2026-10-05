### US-E17-04 — Guide de rédaction et pack d'exemple de tous les modes

**Statut :** À faire

**En tant qu'** auteur de pack
**je veux** un guide qui m'explique pas à pas comment écrire un pack, et un pack d'exemple qui utilise tous les modes
**afin de** créer un pack complet sans lire le code

**Critères d'acceptation**
- Étant donné `docs/guide-packs.md`, quand un auteur le lit, alors il y trouve : la structure d'un pack (dossier, `pack.json`, médias), l'association du schéma dans VS Code, les formats de médias acceptés et la règle de casse, la préparation d'un extrait audio, la vérification avec `validate`, l'aperçu sur la TV, et le partage en zip.
- Étant donné le guide, quand il présente les modes, alors il renvoie à `docs/modes/<mode>.md` pour chacun (quiz, questions buzzer, blind test, question ouverte), chaque page ayant un exemple complet de descripteur à jour.
- Étant donné `packs/soiree-exemple`, quand il est chargé, alors il est valide et enchaîne une manche de chaque mode, avec des médias libres de droits dont la licence est indiquée.
- Étant donné le guide, quand il liste les erreurs courantes, alors chacune montre le message affiché par `validate` et la correction.
- Étant donné les tests, quand ils s'exécutent, alors un test vérifie que chaque pack de `packs/` est valide, et que chaque exemple de descripteur des pages `docs/modes/` est conforme au schéma.

**Comportement en cas d'erreur**
Sans objet : documentation et contenu.

**Notes techniques**
- Vérification de terrain du critère de sortie de la phase 5 : une personne qui n'a pas lu le code écrit un pack à l'aide du guide seul ; ses difficultés corrigent le guide.

**Hors périmètre**
- Un éditeur de pack graphique.
