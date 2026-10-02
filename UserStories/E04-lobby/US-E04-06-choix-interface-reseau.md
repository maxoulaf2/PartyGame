### US-E04-06 — Choix de l'interface réseau par le GM

**Statut :** Terminée

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
- Réalisation : contrats `ChooseAdvertisedAddressRequest(address)`, `ChooseAdvertisedAddressResult(refusal)` et codes `ChooseAdvertisedAddressRefusal` : `AddressUnknown`, `ChoiceFailed` (bug du moteur), `MessageInvalid`. `GameMasterSnapshot` porte `JoinAddress` et `JoinAddressCandidates` (`GameMasterJoinAddress(address, interfaceName)`) ; `DisplaySnapshot` est inchangé et ne porte que l'adresse retenue.
- Réalisation : `GameState` porte `JoinAddressCandidates`, rempli au démarrage par `AddressSelection.ToJoinAddressCandidates()` : toutes les candidates détectées, dans l'ordre de préférence, précédées de l'adresse imposée par `Network:AdvertisedAddress` quand aucune candidate ne la porte (sans nom d'interface), pour que le GM puisse y revenir. Le choix ne vit que dans l'état : un redémarrage l'oublie.
- Réalisation : l'intention moteur `ChooseAdvertisedAddress` est traitée par `Lobby/AddressChoice`, dans toutes les phases (les inscriptions restent ouvertes) : refusée (`AddressUnknown`) si l'adresse n'est pas exactement l'une des candidates, acceptée sans changement (donc sans diffusion) si c'est déjà l'adresse annoncée. Le hub expose `ChooseAdvertisedAddress`, marqué `[GameMasterOnly]`, et journalise en `Information` « Address {Address} advertised to phones, chosen by the game master ». La bannière n'est pas réaffichée.
- Réalisation : côté client, `GameMasterSession.chooseAddress` envoie l'intention et traduit la réponse. `gm/AddressControl.svelte`, entre les compteurs et le bouton de lancement, affiche l'adresse et son interface quand il n'y a qu'une candidate, sinon un `<select>` natif (« adresse (interface) », ou « imposée par la configuration »). Le choix reste affiché, sélecteur désactivé, jusqu'au snapshot qui l'annonce ; un refus ou une coupure rend la main au snapshot, seul `ChoiceFailed` affiche un message.
- Réalisation : tests du moteur (`AddressChoiceTests` : choix d'une candidate, même adresse, après le lancement, sans adresse annoncée, adresses refusées, aucune candidate ; `SnapshotsTests` : candidates dans la projection GM, adresse choisie dans les projections TV et GM, non-fuite des candidates et des noms d'interface vers la TV et les joueurs), du serveur (`AddressSelectorTests` : construction des candidates ; `ChooseAdvertisedAddressTests` : candidates réservées au GM, diffusion du choix, refus sans diffusion, adresse imposée puis remplacée et rechoisie, oubli au redémarrage, intention ignorée sans authentification GM, messages malformés), Vitest (`gameMasterSession.test.ts`) et Playwright : `e2e/gm.spec.ts` (une seule candidate sans sélecteur, choix parmi trois, avec un faux hub) et `e2e/address.spec.ts` (choix sur le vrai serveur et changement de l'URL de la TV en moins d'une seconde, puis retour à l'adresse imposée ; ignoré si la machine de test n'a aucune autre adresse privée). Ce dernier change le QR code de toutes les TV du serveur partagé : il tourne dans le projet Playwright `launch`, après tous les autres.
- Réalisation : vérifié sur de vrais appareils, sur le serveur .NET servant le front construit : choix d'une autre adresse depuis la console du GM, QR code et URL de la TV mis à jour sans rechargement, téléphone rejoignant la partie par le nouveau QR code.

**Hors périmètre**
- Détection d'un changement de réseau après le démarrage (US-E02-02).
- Persistance du choix entre deux démarrages.
