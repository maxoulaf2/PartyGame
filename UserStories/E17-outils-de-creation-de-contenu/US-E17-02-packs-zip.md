### US-E17-02 — Packs au format zip

**Statut :** Terminée

**En tant qu'** auteur de pack
**je veux** partager mon pack en un seul fichier zip, que le GM dépose tel quel dans le dossier des packs
**afin de** transmettre un pack et ses médias sans rien oublier

**Critères d'acceptation**
- Étant donné un `.zip` dans le dossier des packs, dont `pack.json` est à la racine ou dans un unique dossier de premier niveau, quand le serveur démarre ou que le GM actualise les packs, alors le pack est chargé et validé comme un dossier, et apparaît dans la console GM avec le nom du fichier zip.
- Étant donné un pack zip lancé, quand la TV demande un média, alors il est servi depuis le cache d'extraction, avec les requêtes partielles (Range), sous un identifiant aléatoire (inchangé par rapport aux dossiers).
- Étant donné un zip inchangé (même taille, même date de modification), quand les packs sont actualisés, alors le cache existant est réutilisé sans nouvelle extraction ; un zip modifié est extrait à nouveau, et le cache d'un zip disparu est supprimé.
- Étant donné un zip corrompu, sans `pack.json`, contenant une entrée qui sort du dossier d'extraction (`../`, chemin absolu) ou dont la taille décompressée dépasse 2 Go, quand il est chargé, alors le pack est marqué invalide avec un problème précis, sans rien écrire hors du cache, et les autres packs se chargent normalement.
- Étant donné un dossier et un zip qui donneraient le même identifiant de pack, quand ils sont chargés, alors les deux sont signalés en conflit, et aucun n'est sélectionnable.
- Étant donné une partie lancée depuis un pack zip, quand le serveur redémarre, alors la partie reprend (E11) avec ses médias, même si le cache a été vidé.
- Étant donné un fichier zip passé à la commande `validate` (US-E17-01), quand elle s'exécute, alors il est vérifié comme un dossier, sans laisser de fichier extrait derrière lui.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent le chargement, la réutilisation du cache, chaque refus ci-dessus, et un test E2E qui joue un extrait audio d'un pack zip.

**Comportement en cas d'erreur**
Contenu invalide : pack non sélectionnable, problème listé au GM avant la partie (US-E06-03). Disque plein pendant l'extraction : pack invalide avec un problème dédié. Joueurs et public : rien.

**Notes techniques**
- `System.IO.Compression.ZipFile`, inclus dans .NET : aucune dépendance ajoutée.
- L'extraction se fait hors de la boucle de jeu, comme le chargement des dossiers ; le moteur ne voit aucune différence.
- La règle de casse des chemins de médias (US-E06-02) s'applique aux entrées du zip.
- Réalisation : `PackArchive` (`PartyGame.Content`) extrait le zip dans `packs-cache/<id>/`, `pack.json` à la racine, et écrit à côté un fichier `<id>.stamp` (taille et date du zip) une fois l'extraction complète ; le dossier `__MACOSX/` du Finder est ignoré. Les entrées et la taille déclarée sont vérifiées avant toute écriture, puis la taille réellement décompressée. Le service des médias cherche le pack dans le dossier des packs, puis dans le cache s'il n'y a pas de `pack.json`. Le test E2E construit son zip avec `e2e/zip.ts` (zip sans compression, sans dépendance).

**Hors périmètre**
- Téléverser un zip depuis la console GM.
- Créer le zip d'un pack (un outil d'archivage du système suffit ; le guide l'explique).
