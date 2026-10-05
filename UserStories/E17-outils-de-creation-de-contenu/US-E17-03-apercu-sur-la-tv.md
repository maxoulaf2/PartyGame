### US-E17-03 — Aperçu d'un pack sur la TV

**Statut :** À faire

**En tant qu'** auteur de pack ou game master
**je veux** faire défiler chaque question d'un pack sur la TV, réponse comprise, avant la soirée
**afin de** vérifier la mise en page, les images et les extraits audio tels que le public les verra

**Critères d'acceptation**
- Étant donné le lobby et un pack valide, quand le GM appuie sur « Aperçu » à côté du pack, alors la console avertit « Les réponses s'afficheront sur la TV » et, après confirmation, la TV passe en aperçu sur la première question de la première manche.
- Étant donné l'aperçu, quand la TV affiche une étape, alors elle montre la question comme pendant la partie, puis sa révélation (bonne réponse, titre et artiste, visuel), avec le bandeau « Aperçu » et la position (« Manche 2/3 · Question 4/10 »).
- Étant donné la console GM en aperçu, quand le GM utilise « Précédente », « Suivante » ou choisit une manche, alors la TV suit ; sur un extrait de blind test, « Écouter l'extrait » le joue sur la TV avec son début et sa durée.
- Étant donné une image ou un extrait que la TV n'arrive pas à charger, quand l'aperçu l'affiche, alors la console GM signale l'incident avec la manche et la question, comme en partie.
- Étant donné l'aperçu en cours, quand des joueurs rejoignent ou sont déjà inscrits, alors leurs téléphones restent sur le lobby et ne reçoivent rien du pack.
- Étant donné le GM qui appuie sur « Quitter l'aperçu », ou qui lance la partie, quand l'intention est traitée, alors la TV revient au lobby. Une demande d'aperçu une fois la partie lancée est rejetée.
- Étant donné les tests, quand ils s'exécutent, alors ils couvrent l'entrée, la navigation et la sortie de l'aperçu, ses rejets, un test de non-fuite qui vérifie que la projection `Player` ne contient rien du pack, et un scénario E2E qui parcourt un pack de chaque mode.

**Comportement en cas d'erreur**
Public : en cas d'échec d'une vue d'aperçu, la TV affiche l'écran d'attente et le GM passe à l'étape suivante. Joueurs : rien. GM : une intention obsolète est rejetée sans effet.

**Notes techniques**
- L'aperçu vit dans l'état du lobby (pack, manche, question), n'est pas une partie et n'attribue aucun point ; il n'est pas repris après un redémarrage.
- Chaque mode fournit la liste de ses étapes d'aperçu et une vue `Display` révélée pour chacune, à partir de son descripteur, en réutilisant ses vues de partie : le moteur et la TV n'ajoutent qu'un conteneur générique, sans connaître les modes.
- Les médias de l'aperçu sont servis sous `/media/<identifiant>` comme en partie, avec des identifiants tirés à l'entrée dans l'aperçu.

**Hors périmètre**
- Rafraîchir l'aperçu automatiquement quand le pack change sur le disque (« Actualiser les packs » suffit).
- Jouer l'aperçu sur les téléphones.
