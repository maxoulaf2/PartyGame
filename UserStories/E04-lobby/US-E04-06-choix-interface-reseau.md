### US-E04-06 — Choix de l'interface réseau par le GM

**Statut :** À faire

**En tant que** game master
**je veux** choisir depuis mon interface l'adresse encodée dans le QR code quand le PC est connecté à plusieurs réseaux
**afin de** corriger un mauvais choix automatique sans redémarrer le serveur ni modifier la configuration

**Critères d'acceptation**
- Étant donné un serveur avec plusieurs adresses candidates (US-E02-02), quand le GM ouvre le lobby, alors il voit l'adresse annoncée et peut en choisir une autre dans la liste des candidates, chacune identifiée par l'adresse et le nom de son interface.
- Étant donné une seule candidate, quand le GM ouvre le lobby, alors il voit l'adresse annoncée sans sélecteur.
- Étant donné le GM qui choisit une autre candidate, quand le choix est accepté, alors le QR code et l'URL de la TV changent en moins d'une seconde, sans rechargement.
- Étant donné une adresse imposée par `Network:AdvertisedAddress`, quand le GM ouvre le lobby, alors elle est présentée comme le choix courant, et il peut tout de même choisir une candidate.
- Étant donné une intention qui désigne une adresse absente de la liste des candidates, quand le serveur la reçoit, alors elle est rejetée (log `Debug`) : le GM ne peut pas saisir une adresse arbitraire.
- Étant donné un redémarrage du serveur, quand il démarre, alors le choix fait par le GM est oublié et la sélection automatique (ou la configuration) s'applique de nouveau.
- Étant donné les projections, quand les tests de non-fuite s'exécutent, alors seule la projection `GameMaster` contient la liste des candidates ; la projection `Display` ne contient que l'adresse retenue.

**Comportement en cas d'erreur**
GM : un choix rejeté laisse l'affichage inchangé, le snapshot fait foi. Public : la TV affiche toujours un QR code valide ou le message neutre de US-E02-03. Joueurs déjà inscrits : aucun impact, leur connexion utilise l'adresse par laquelle ils sont arrivés.

**Notes techniques**
- Intention GM `ChooseAdvertisedAddress(address)`. Les candidates sont injectées dans l'état au démarrage depuis `AddressSelection` ; le moteur ne fait que valider l'appartenance.
- Le choix n'est pas écrit dans la configuration. La bannière de la console n'est pas réaffichée ; un log `Information` indique le changement.
- Prévue par la décision 1 de [E02](../E02-acces-des-appareils/README.md).

**Hors périmètre**
- Détection d'un changement de réseau après le démarrage (US-E02-02).
- Persistance du choix entre deux démarrages.
