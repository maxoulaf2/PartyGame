# Dispersion des horloges

Mesure demandée par US-E13-06 pour la phase 4 : l'écart entre les horloges des téléphones, une fois synchronisées sur le serveur, borne la justesse du départage au buzzer.

## Protocole

1. Lancer le serveur sur le PC hôte et connecter les appareils au Wi-Fi du lieu.
2. Ouvrir `/diagnostic/` sur chaque téléphone (au moins un iPhone sous Safari et un Android sous Chrome) et sur l'écran TV, attendre la fin du test, et noter pour chacun l'aller-retour et l'incertitude affichés dans la section « Horloge ».
3. Poser les appareils côte à côte, toucher « Flash synchronisé » sur chacun.
4. Filmer l'ensemble au ralenti (240 images/s si possible, soit environ 4 ms par image) pendant une dizaine de flashs.
5. Pour chaque flash, compter les images entre le premier et le dernier appareil qui s'allume, et en déduire l'écart en millisecondes. Retenir l'écart médian et l'écart maximal.

L'écart mesuré inclut la latence d'affichage propre à chaque appareil (une à deux images d'écran, soit 16 à 33 ms à 60 Hz) : il majore l'écart réel des horloges.

## Résultats

À compléter après la mesure sur de vrais appareils.

| Date | Lieu et Wi-Fi | Appareils | Aller-retour / incertitude affichés | Écart médian | Écart maximal |
|---|---|---|---|---|---|
| | | | | | |
