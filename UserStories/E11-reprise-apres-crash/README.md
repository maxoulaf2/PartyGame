# E11 — Reprise après crash

**Phase :** 3. Résilience
**Objectif :** un crash du serveur, un PC qui redémarre ou un arrêt par erreur ne coûtent qu'une brève reconnexion. L'état de la partie est enregistré après chaque transition ; au redémarrage, le GM se voit proposer de reprendre la partie, et les téléphones, la TV et la console GM la retrouvent là où elle en était, sans que les joueurs fassent quoi que ce soit.

## User stories

| US | Titre | Statut | Dépend de |
|---|---|---|---|
| [US-E11-01](US-E11-01-persistance-de-l-etat.md) | Enregistrement de la partie après chaque transition | Terminée | — |
| [US-E11-02](US-E11-02-proposition-de-reprise.md) | Proposition de reprise au GM | Terminée | US-E11-01, US-E10-01 |
| [US-E11-03](US-E11-03-reprise-transparente.md) | Reprise transparente pour les joueurs et la TV | Prête | US-E11-02 |

## Décisions

Toutes les décisions qui bloquaient l'épopée sont prises. Les valeurs numériques ci-dessous sont des valeurs de départ, ajustables sans nouvelle décision.

1. **Reprise confirmée par le GM :** au redémarrage, une partie enregistrée n'est pas reprise d'office. La console GM propose « Reprendre la partie » ou « Nouvelle partie ». En attendant, les téléphones restent sur « Retour dans la partie… » et la TV sur un écran d'attente. Option écartée : une reprise automatique, sans action du GM, qui aurait laissé « Nouvelle partie » à E19 ou à la suppression d'un fichier à la main.
2. **Le compte à rebours reprend le temps qui restait :** une question dont les réponses étaient ouvertes repart avec le temps qui restait au dernier enregistrement. La nouvelle échéance est l'instant de la reprise plus ce temps restant, et les réponses déjà reçues gardent leur bonus de rapidité. Personne n'est pénalisé par la coupure. Options écartées : l'échéance absolue conservée, qui verrouille les réponses dès la reprise après une longue coupure, et la question relancée depuis sa présentation, réponses effacées.
3. **Nouveau code GM après un redémarrage :** le code GM n'est jamais enregistré avec la partie. Il est régénéré à chaque démarrage, comme le veut CLAUDE.md, et la console GM le redemande avant de proposer la reprise. Option écartée : le code de la partie enregistré avec elle, qui aurait écrit le code sur disque.
4. **Ce qui est proposé à la reprise :** toute partie enregistrée qui compte au moins un joueur, du lobby jusqu'au classement final. Un lobby vide n'est pas proposé. « Nouvelle partie » met l'ancien fichier de côté (`previous-game.json`), pour qu'une erreur de manipulation reste récupérable à la main. Choix de réalisation, ajustable sans nouvelle décision.

## Ordre de réalisation suggéré

1. US-E11-01, en parallèle de E10.
2. US-E11-02, puis US-E11-03.

## Critère de sortie (phase 3)

Porté par E12 : si on tue le serveur en pleine question, la partie reprend sans que les joueurs fassent quoi que ce soit ; si on injecte une exception, seul le GM en est informé.
