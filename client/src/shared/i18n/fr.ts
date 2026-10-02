// Every text shown to players, the TV screen and the game master lives here:
// components never hard-code user-facing strings.
export const fr = {
    app: {
        name: 'PartyGame',
    },
    player: {
        waiting: 'Bienvenue ! La partie va bientôt commencer.',
    },
    display: {
        waiting: 'En attente des joueurs…',
        scanToJoin: 'Scannez pour rejoindre la partie',
        typeAddress: 'ou ouvrez cette adresse :',
        qrCodeLabel: 'QR code pour rejoindre la partie',
        // Shown instead of the QR code when the server knows no address phones can reach.
        joinUnavailable: 'Préparation de la connexion des joueurs…',
        playersJoined: {
            zero: 'En attente des joueurs…',
            one: '{count} joueur inscrit',
            other: '{count} joueurs inscrits',
        },
    },
    gm: {
        waiting: 'Console du game master : connexion en cours…',
        consoleTitle: 'Console du game master',
        code: {
            title: 'Code game master',
            instructions: 'Saisissez le code à 6 chiffres affiché dans la console du serveur.',
            label: 'Code à 6 chiffres',
            submit: 'Valider',
            // Neutral on purpose: says nothing about which digits are wrong.
            invalid: 'Code incorrect',
            expired: 'Le code a changé : relisez-le dans la console du serveur.',
        },
        playersJoined: {
            zero: 'Aucun joueur inscrit pour l’instant',
            one: '{count} joueur inscrit',
            other: '{count} joueurs inscrits',
        },
    },
} as const;
