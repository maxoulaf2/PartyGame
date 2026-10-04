### US-E12-02 — Table complète sur un serveur dédié en E2E

**Statut :** Terminée

Un test E2E réunit l'écran TV, la console GM et trois joueurs sur des téléphones émulés, contre un serveur qui lui est propre. Il teste ainsi de bout en bout une partie, un redémarrage ou une reprise, sans dépendre de l'ordre des autres tests.

**Critères d'acceptation**
- Étant donné un test E2E, quand il demande une table, alors il obtient l'écran TV (bureau), la console GM déjà authentifiée, et trois joueurs inscrits, chacun dans son propre contexte de navigateur : au moins un iPhone sous WebKit et un Pixel sous Chromium dans la même partie.
- Étant donné un test qui demande un serveur dédié, quand il démarre, alors un serveur .NET est lancé pour lui seul, sur un port libre, avec son propre dossier de données et les packs de `e2e/packs`, depuis le build déjà produit (pas de `dotnet run` par test). Il est arrêté à la fin du test, même en cas d'échec.
- Étant donné un serveur dédié, quand le test le demande, alors il peut l'arrêter brutalement (processus tué) puis le relancer sur le même dossier de données et le même port : c'est la brique des tests de redémarrage de US-E12-03.
- Étant donné le scénario de partie complète (`launch.spec.ts`), quand il s'exécute, alors il joue sur son serveur dédié avec trois joueurs, et n'a plus besoin de passer après tous les autres tests : le projet Playwright `launch` disparaît.
- Étant donné un test qui échoue, quand le rapport est produit, alors il contient les logs du serveur dédié et une capture de chaque page de la table.
- Étant donné les tests E2E existants, quand ils s'exécutent, alors ils passent toujours, en parallèle des scénarios sur serveur dédié.

**Comportement en cas d'erreur**
Sans objet : outillage de test. Un serveur dédié qui ne démarre pas fait échouer le test avec ses logs.

**Notes techniques**
- Une fixture Playwright (`e2e/fixtures/`) fournit `dedicatedServer` et `table`. Le serveur dédié sert lui-même le front construit dans `wwwroot` : les pages s'ouvrent directement sur lui, sans le serveur de prévisualisation de Vite.
- Le Pixel et l'iPhone d'une même table viennent de deux moteurs : la fixture lance WebKit à côté du navigateur du projet. L'installation `npx playwright install chromium webkit` reste suffisante.
- Le code GM du serveur dédié est fixé par `GameMaster:Code`, comme aujourd'hui.
- Le projet `address` peut suivre le même chemin ; à décider pendant l'US selon le gain. Mettre à jour la section « Commandes » de CLAUDE.md.
- Une partie E2E plus longue qu'aujourd'hui allonge `npm run e2e` : garder des packs de test courts et des comptes à rebours brefs.
- Réalisation : `e2e/fixtures/table.ts` fournit `dedicatedServer` (le `PartyGame.Server.dll` construit par le `dotnet run` du serveur partagé, lancé depuis `src/PartyGame.Server` pour servir `wwwroot`, sur un port libre, avec un dossier temporaire de données et de logs ; `kill` tue le processus, `start` le relance sur le même port et le même dossier) et `table` (TV et console GM sur Chrome bureau, Zoé sur un iPhone WebKit, Max et Léa sur des Pixel Chromium). La fixture redéfinit `baseURL` : toutes les pages du test s'ouvrent sur son serveur. En cas d'échec, `server.log` et une capture par page de la table sont joints au rapport et écrits dans `test-results/`. `launch.spec.ts` joue sur une table, avec des décomptes exacts (4 participants, 6 joueurs classés) ; `address.spec.ts` passe aussi sur un serveur dédié, ce qui supprime les projets `address` et `launch` : les deux tournent dans `desktop-chrome`, en parallèle des autres, et `npm run e2e` ne les attend plus. `dedicatedServer.spec.ts` vérifie l'arrêt brutal et la relance : la console se voit proposer de reprendre le lobby enregistré.

**Hors périmètre**
- Les scénarios de chaos (US-E12-03).
- Les tests sur de vrais appareils, toujours manuels.
