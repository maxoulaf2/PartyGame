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
    },
    gm: {
        waiting: 'Console du game master : en attente de la partie.',
    },
} as const;
