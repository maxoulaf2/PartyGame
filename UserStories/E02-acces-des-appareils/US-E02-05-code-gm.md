### US-E02-05 — Code GM généré au démarrage

**Statut :** Prête

**En tant que** game master
**je veux** qu'un code secret soit généré au démarrage et affiché uniquement dans la console du serveur
**afin que** moi seul puisse piloter la partie, même si n'importe qui sur le réseau peut ouvrir `/gm/`

**Critères d'acceptation**
- Étant donné un démarrage du serveur sans code configuré, quand il est prêt, alors un code GM de 6 chiffres est généré avec un générateur cryptographique et affiché dans la bannière de démarrage de la console.
- Étant donné deux démarrages successifs, quand on compare les codes, alors ils diffèrent (sauf coïncidence, avec une chance sur un million).
- Étant donné un code imposé par la configuration (`GameMaster:Code`), quand le serveur démarre, alors ce code est utilisé, et la bannière indique qu'il provient de la configuration. Un code configuré qui ne respecte pas le format arrête le serveur au démarrage avec un message clair.
- Étant donné les fichiers de `logs/`, quand le serveur a démarré, alors le code GM n'y apparaît pas. Il est écrit sur la console seulement, hors du logger.
- Étant donné les pages et les réponses HTTP servies (`/display/`, `/api/join`, `/health`…), quand on les inspecte, alors aucune ne contient le code GM.
- Étant donné le service de vérification du code, quand on lui soumet le bon code, un mauvais code, un code vide ou un code avec des espaces autour, alors il répond respectivement vrai, faux, faux et vrai. La comparaison se fait en temps constant.

**Comportement en cas d'erreur**
Opérateur : un code configuré invalide empêche le démarrage, avec un message qui nomme la clé de configuration. Joueurs, public et GM : sans objet, car le code n'est encore vérifié par aucune intention.

**Notes techniques**
- Le code est fourni par un service singleton (par exemple `GameMasterCode`) qui expose la vérification et jamais la valeur, sauf à la bannière. Le hub (E03) l'utilisera pour refuser les intentions GM.
- La génération utilise `RandomNumberGenerator` côté serveur. La contrainte de « secure context » ne concerne que les navigateurs.
- La bannière est commune avec US-E02-02 : le code y figure à côté des URL de l'écran TV et du GM.
- Les tests E2E pourront fixer le code par configuration.
- Dépend de US-E02-01 (bannière de démarrage).

**Hors périmètre**
- Écran de saisie du code dans l'interface GM et refus des intentions GM sans code (E03 et E04).
- Limitation du nombre de tentatives (avec la vérification dans le hub, E03).
- Conservation du code après une reprise sur crash : le code est régénéré, et le GM le relit dans la console (décision 3 du README).
