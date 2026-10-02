# E09 — Scores

**Phase :** 2. Premier mode : quiz QCM
**Objectif :** le moteur attribue les points selon le barème du mode, les cumule sur toute la partie, et calcule les classements. Les joueurs voient ce qu'ils gagnent à chaque question, la TV affiche le classement entre deux manches puis le classement final, et le GM suit les scores à tout moment.

## User stories

| US | Titre | Statut | Dépend de |
|---|---|---|---|
| [US-E09-01](US-E09-01-points-par-question.md) | Points gagnés à chaque question | À faire | US-E08-04 |
| [US-E09-02](US-E09-02-classement-intermediaire.md) | Classement entre deux manches | À faire | US-E09-01, US-E08-05 |
| [US-E09-03](US-E09-03-classement-final.md) | Classement final | À faire | US-E09-02 |

## Décisions

Toutes les décisions qui bloquaient l'épopée sont prises.

1. **Barème du quiz : points fixes et bonus de rapidité facultatif.** Une bonne réponse rapporte les points de la manche (`points`, 1 000 par défaut). Si la manche définit un bonus (`speedBonus`, 0 par défaut), il s'y ajoute en proportion du temps restant : bonus × temps restant ÷ durée de réponse, arrondi à l'entier. Une mauvaise réponse ou une absence de réponse rapporte 0, sans pénalité. Options écartées : des points fixes sans bonus possible, et un bonus de rapidité imposé.
2. **Temps de réponse mesuré à la réception par le serveur :** le temps restant d'une réponse se mesure entre son heure de réception (`ReceivedAt`) et l'échéance. Sur un réseau local, l'écart de latence entre téléphones, de quelques millisecondes, est négligeable devant une durée de réponse de plusieurs secondes. L'horodatage côté client reste réservé au buzzer (E13), où l'écart se joue en millisecondes.
3. **Points attribués à la révélation :** les points d'une question s'ajoutent aux scores au moment de la révélation, pas du verrouillage. Une question passée avant sa révélation ne rapporte rien, et aucun score ne peut trahir une bonne réponse avant l'heure.
4. **Joueurs arrivés en cours de partie :** ils commencent à 0 point et figurent dans les classements (décision 5 du README de E08). La règle définitive reste à trancher en phase 6.
5. **Ex aequo :** des joueurs à égalité partagent le même rang, et le rang suivant tient compte de leur nombre (1, 1, 3). À égalité, l'affichage suit l'ordre alphabétique des pseudos. Choix de réalisation, ajustable sans nouvelle décision.

## Ordre de réalisation suggéré

1. US-E09-01, dès que la révélation existe.
2. US-E09-02, puis US-E09-03.

## Critère de sortie (phase 2)

Une partie avec le pack d'exemple (10 questions en deux manches, US-E08-01) est jouée de bout en bout par trois joueurs sur de vrais téléphones (au moins un iPhone sous Safari et un Android sous Chrome), avec l'écran TV et la console GM : choix du pack, lancement, deux manches, classement intermédiaire et classement final. La vérification est faite avec le serveur lancé par `scripts/start.ps1`, sur le Wi-Fi d'un vrai réseau domestique.

Les tests de non-fuite passent pour chaque phase et chaque rôle (US-E07-03). `dotnet test`, `dotnet format --verify-no-changes`, `npm run check`, `npm run test` et `npm run e2e` passent.
