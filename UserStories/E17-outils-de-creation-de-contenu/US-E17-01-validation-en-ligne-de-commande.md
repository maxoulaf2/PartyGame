### US-E17-01 — Validation d'un pack en ligne de commande

**Statut :** Terminée

**En tant qu'** auteur de pack
**je veux** vérifier mon pack d'une commande, sans lancer de partie ni ouvrir de navigateur
**afin de** corriger toutes ses erreurs pendant que je l'écris

**Critères d'acceptation**
- Étant donné un dossier de pack valide, quand l'auteur lance `PartyGame.Server validate <chemin>` (ou `dotnet run --project src/PartyGame.Server -- validate <chemin>`), alors la commande affiche le titre du pack, ses manches (titre et mode) et « Pack valide », puis se termine avec le code de sortie 0, sans écouter le réseau.
- Étant donné un pack invalide, quand la commande s'exécute, alors elle affiche chaque problème en français, avec le fichier, le chemin dans le descripteur et le code (décision 4 du README), puis se termine avec le code 1.
- Étant donné un dossier qui contient plusieurs packs (le dossier `packs/`, par exemple), quand la commande s'exécute, alors elle vérifie chacun et résume « 3 packs valides, 1 invalide » ; le code de sortie est 1 si l'un est invalide.
- Étant donné un fichier zip (US-E17-02), quand la commande s'exécute, alors il est vérifié comme un dossier, sans laisser de fichier extrait derrière lui. *Reporté à US-E17-02, qui introduit les zip : en attendant, un zip est signalé comme n'étant ni un pack ni un dossier de packs (code 2).*
- Étant donné un chemin inexistant, ou qui n'est ni un pack ni un dossier de packs, quand la commande s'exécute, alors elle l'indique clairement et se termine avec le code 2.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent les quatre cas ci-dessus et vérifient que chaque code de problème a un message français dans la table .NET.

**Comportement en cas d'erreur**
Auteur : jamais de pile d'appels pour un problème de contenu ; une exception inattendue affiche un message court et le code 3, détail dans les logs.

**Notes techniques**
- La commande réutilise `PackLoader` et la vérification de chaque mode (`IGameMode.Validate`) : elle ne peut pas diverger du chargement du serveur.
- Pas de bannière ni de code GM : la commande s'exécute avant la construction de l'hôte web.
- `CLAUDE.md` (section Commandes) et `docs/installation.md` la mentionnent.

**Hors périmètre**
- Surveiller le dossier et revalider à chaque modification.
- Une sortie JSON pour l'intégration continue.
