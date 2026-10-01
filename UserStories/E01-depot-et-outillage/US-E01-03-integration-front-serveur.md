### US-E01-03 — Build du front vers `wwwroot` et proxy de développement

**Statut :** Prête

**Résultat attendu**
Le serveur .NET sert le front construit, et en développement Vite est exposé sur le réseau local et relaie vers le serveur .NET les requêtes qu'il ne traite pas lui-même.

**Critères d'acceptation**
- Étant donné le client, quand on lance `npm run build` depuis `client/`, alors les fichiers sont produits dans `src/PartyGame.Server/wwwroot`, avec un HTML par point d'entrée et des ressources dont le nom contient une empreinte (hash).
- Étant donné un build présent dans `wwwroot`, quand on lance `dotnet run --project src/PartyGame.Server`, alors les pages `player`, `display` et `gm` sont servies par le serveur .NET.
- Étant donné `npm run dev`, quand on ouvre l'URL affichée par Vite depuis un autre appareil du réseau local, alors la page s'affiche (Vite écoute sur toutes les interfaces).
- Étant donné `npm run dev` et le serveur .NET lancés, quand le navigateur appelle `/hub/...` ou `/media/...` sur le port de Vite, alors la requête est relayée au serveur .NET, WebSockets compris pour `/hub`.
- Étant donné le dépôt, quand on construit le front, alors le contenu généré de `wwwroot` n'apparaît pas dans `git status`.
- Étant donné un serveur lancé sans build du front, quand on ouvre une page, alors le serveur répond par une 404 et journalise un avertissement explicite au démarrage, sans planter.

**Comportement en cas d'erreur**
Sans objet pour les joueurs, le public et le GM. Si `wwwroot` est vide, l'opérateur voit un avertissement au démarrage indiquant de lancer `npm run build`.

**Notes techniques**
- Les fichiers HTML ne sont pas mis en cache (ou revalidés), les ressources à empreinte le sont longtemps : prépare le rechargement automatique sur changement de build (E05).
- Ports de développement à documenter dans la section « Commandes » de CLAUDE.md.
- Dépend de US-E01-01 et US-E01-02.

**Hors périmètre**
- Routes `/`, `/display`, `/gm` et écoute sur `0.0.0.0` côté Kestrel (E02).
- Service des médias avec requêtes partielles (E14) : seul le proxy `/media` est mis en place ici.
- Intégration du build du front dans `dotnet publish` (E21).
