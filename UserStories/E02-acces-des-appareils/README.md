# E02 — Accès des appareils

**Phase :** 0. Socle technique
**Objectif :** un téléphone du réseau local scanne le QR code affiché sur la TV et ouvre la page joueur, sans configuration ni saisie d'adresse. L'opérateur trouve dans la console tout ce qu'il lui faut pour lancer la soirée : adresses de l'écran TV et du GM, et code GM.

## User stories

| US | Titre | Statut | Dépend de |
|---|---|---|---|
| [US-E02-01](US-E02-01-ecoute-reseau-local.md) | Écoute sur le réseau local | Terminée | — |
| [US-E02-02](US-E02-02-detection-ip-privee.md) | Détection de l'adresse IPv4 privée | Terminée | US-E02-01 |
| [US-E02-03](US-E02-03-qr-code-ecran-tv.md) | QR code sur l'écran TV | En cours | US-E02-02 |
| [US-E02-04](US-E02-04-routage-interfaces.md) | Routage des trois interfaces | Terminée | — |
| [US-E02-05](US-E02-05-code-gm.md) | Code GM généré au démarrage | Terminée | US-E02-01 |

## Décisions

Toutes les décisions qui bloquaient l'épopée sont prises.

1. **Choix de l'interface réseau quand plusieurs sont actives :** le serveur la sélectionne automatiquement, l'opérateur peut la corriger par configuration (`Network:AdvertisedAddress`) et la console liste les autres candidates. Le choix depuis l'interface GM est reporté au lobby (E04), une fois le hub et le code GM en place. Options écartées : un sélecteur sur l'écran TV, qui est public et laisserait n'importe qui changer l'adresse ; un sélecteur GM dès E02, qui obligerait à créer une authentification GM avant le hub.
2. **Génération du QR code :** elle se fait côté client avec `uqr` (MIT, aucune dépendance, sortie SVG, aucun appel réseau). Le client compose l'URL avec le port de la page qu'il a chargée, pour que le QR code soit juste en développement comme en production. Options écartées : `qrcode`, qui tire trois dépendances (`pngjs`, `yargs`, `dijkstrajs`) ; `QRCoder` côté serveur, qui ne connaît pas le port vu par le navigateur derrière le proxy Vite ; un encodeur maison, dont l'effort serait disproportionné.
3. **Format et durée de vie du code GM :** le code compte 6 chiffres et il est régénéré à chaque démarrage. Une valeur fixe peut être imposée par configuration (`GameMaster:Code`) pour le développement et les tests E2E. Après une reprise sur crash (E11), le GM ressaisit le nouveau code, lisible dans la console. Options écartées : un code persisté dans `data/`, qui laisserait le secret sur disque ; une phrase de passe, trop longue à saisir sur un téléphone.

## Ordre de réalisation suggéré

1. US-E02-01 et US-E02-04, en parallèle.
2. US-E02-05 et US-E02-02, qui partagent la bannière de démarrage.
3. US-E02-03, qui porte le critère de sortie de la phase 0.

## Critère de sortie (phase 0)

Un iPhone et un téléphone Android scannent le QR code affiché sur la TV et ouvrent la page joueur. `dotnet test` et `npm run check` passent. La vérification est faite avec le serveur lancé par `dotnet run`, sur le Wi-Fi d'un vrai réseau domestique.
