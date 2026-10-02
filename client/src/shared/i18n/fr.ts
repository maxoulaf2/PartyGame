// Every text shown to players, the TV screen and the game master lives here:
// components never hard-code user-facing strings.
export const fr = {
    app: {
        name: 'PartyGame',
    },
    player: {
        waiting: 'Bienvenue ! La partie va bientôt commencer.',
        started: 'La partie est lancée : garde un œil sur l’écran !',
        registeredAs: 'Tu es inscrit sous le nom {nickname}',
        join: {
            title: 'Choisis ton pseudo',
            label: 'Pseudo',
            submit: 'Rejoindre',
            problems: {
                tooLong: 'Ton pseudo ne peut pas dépasser 16 caractères.',
                invalidCharacters: 'Ton pseudo contient des caractères non autorisés.',
                invalid: 'Ce pseudo n’est pas accepté : de 1 à 16 caractères visibles.',
                taken: 'Ce pseudo est déjà pris : choisis-en un autre.',
                failed: 'L’inscription n’a pas abouti : réessaie.',
            },
        },
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
        playerListLabel: 'Joueurs inscrits',
        // Read by screen readers next to the icon of a player whose phone is disconnected.
        disconnected: 'déconnecté',
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
        playersConnected: {
            zero: 'aucun connecté',
            one: '{count} connecté',
            other: '{count} connectés',
        },
        playerListLabel: 'Joueurs inscrits',
        connected: 'Connecté',
        disconnected: 'Déconnecté',
        rename: {
            action: 'Renommer',
            // Read by screen readers: several « Renommer » buttons must be told apart.
            actionFor: 'Renommer {nickname}',
            label: 'Nouveau pseudo de {nickname}',
            submit: 'Valider',
            cancel: 'Annuler',
            problems: {
                tooLong: 'Le pseudo ne peut pas dépasser 16 caractères.',
                invalidCharacters: 'Le pseudo contient des caractères non autorisés.',
                invalid: 'Ce pseudo n’est pas accepté : de 1 à 16 caractères visibles.',
                taken: 'Ce pseudo est déjà pris par un autre joueur.',
                unknown: 'Ce joueur est introuvable.',
                failed: 'Le renommage n’a pas abouti : réessayez.',
            },
        },
    },
} as const;
