### US-E01-05 — Logs Serilog vers la console et un fichier tournant

**Statut :** Terminée

**Résultat attendu**
Le serveur journalise des messages structurés vers la console et vers un fichier tournant dans `logs/`, avec des niveaux réglables par configuration.

**Critères d'acceptation**
- Étant donné le serveur lancé, quand il démarre, alors un message `Information` de cycle de vie apparaît dans la console et dans un fichier de `logs/`.
- Étant donné plusieurs jours de fonctionnement ou un fichier volumineux, quand la limite est atteinte, alors un nouveau fichier est créé et les plus anciens sont supprimés au-delà d'un nombre de fichiers conservés.
- Étant donné `appsettings.json` et `appsettings.Development.json`, quand on modifie le niveau minimal, alors il s'applique sans recompiler. En développement, `Debug` est visible ; en production, le niveau par défaut est `Information`.
- Étant donné un message avec un gabarit (`{PlayerId}`), quand il est écrit dans le fichier, alors la propriété est conservée de façon structurée et non seulement interpolée dans le texte.
- Étant donné une exception non gérée au démarrage, quand le serveur s'arrête, alors elle est journalisée en `Fatal` et les logs sont vidés avant la sortie.
- Étant donné le dépôt, quand le serveur a tourné, alors `logs/` n'apparaît pas dans `git status`.

**Comportement en cas d'erreur**
Si le dossier `logs/` n'est pas accessible en écriture, le serveur continue de journaliser dans la console et démarre normalement : un problème de log ne doit jamais empêcher une partie.

**Notes techniques**
- Dépendances NuGet à signaler : `Serilog.AspNetCore` et `Serilog.Sinks.File`. Aucun sink réseau.
- Chemin de `logs/` relatif au dossier de l'exécutable, pour fonctionner aussi sur le Raspberry Pi.
- Prévoir dès maintenant de réduire le bruit des catégories `Microsoft.AspNetCore` (niveau `Warning`).
- Dépend de US-E01-01.

**Hors périmètre**
- Remontée des erreurs client via `ReportClientError` (E10).
- Journal des incidents présenté au GM (E10).
