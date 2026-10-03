// Every text shown to players, the TV screen and the game master lives here:
// components never hard-code user-facing strings.
export const fr = {
    app: {
        name: 'PartyGame',
    },
    connection: {
        // Shown on every page once the server has been out of reach for a few seconds.
        reconnecting: 'Reconnexion…',
    },
    // The progress of the game, the same on every page.
    game: {
        round: 'Manche {number}/{count}',
        roundEnded: 'Fin de la manche {number}/{count}',
        finished: 'Partie terminée',
    },
    player: {
        waiting: 'Bienvenue ! La partie va bientôt commencer.',
        // Shown during the game when the phone has nothing more precise to show.
        inProgress: 'La partie est en cours : garde un œil sur l’écran !',
        // Provisional, until the rankings of E09.
        betweenRounds: 'La suite arrive bientôt : garde un œil sur l’écran !',
        finished: 'Merci d’avoir joué !',
        registeredAs: 'Tu es inscrit sous le nom {nickname}',
        // Shown while a phone that joined before waits to be recognized by the server.
        resuming: 'Retour dans la partie…',
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
        // The title of the pack chosen by the game master, shown in the lobby.
        packTitle: 'Au programme : {title}',
        // Read by screen readers next to the icon of a player whose phone is disconnected.
        disconnected: 'déconnecté',
        // Shown during the game when the TV screen has nothing more precise to show.
        inProgress: 'Partie en cours',
        // Provisional, until the rankings of E09.
        betweenRounds: 'La manche suivante arrive bientôt',
        finished: 'Merci d’avoir joué !',
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
        address: {
            label: 'Adresse des joueurs',
            // One address the QR code may encode, with the network it belongs to.
            option: '{address} ({origin})',
            // Origin of an address imposed by Network:AdvertisedAddress that no interface holds.
            configured: 'imposée par la configuration',
            hint: 'Le QR code de l’écran TV encode cette adresse : choisissez celle du Wi-Fi des joueurs.',
            none: 'Aucune adresse de réseau local : connectez ce PC au Wi-Fi, puis relancez le serveur.',
            failed: 'Le changement d’adresse n’a pas abouti : réessayez.',
        },
        packs: {
            title: 'Pack de la partie',
            directory: 'Dossier des packs : {directory}',
            none: 'Aucun pack trouvé',
            noneHint:
                'Un pack est un sous-dossier qui contient un fichier pack.json. Ajoutez-en un, puis actualisez.',
            listLabel: 'Packs disponibles',
            untitled: 'Pack sans titre',
            folder: 'Dossier {folder}',
            valid: 'Valide',
            invalid: {
                zero: 'Invalide',
                one: 'Invalide : {count} problème',
                other: 'Invalide : {count} problèmes',
            },
            roundCount: {
                zero: 'Aucune manche',
                one: '{count} manche',
                other: '{count} manches',
            },
            // One round of a pack, with the game mode that plays it.
            round: '{title} ({mode})',
            showProblems: 'Voir les problèmes',
            // Where a problem is: the file of the pack, then the JSON path within it, as written.
            location: '{file}, {path}',
            reload: 'Actualiser les packs',
            reloading: 'Actualisation…',
            reloadHint: 'Après avoir modifié un pack sur le disque, actualisez pour le revérifier.',
            reloadFailed: 'L’actualisation n’a pas abouti : réessayez.',
            selectFailed: 'Le choix du pack n’a pas abouti : réessayez.',
            // Once the game is started, its pack is fixed.
            played: 'Pack : {title}',
            // The game modes, by the type of activity of the packs; an unknown one shows as is.
            modes: {
                quiz: 'Quiz QCM',
            },
            // What a value should be, inserted in the message of PackValueTypeInvalid.
            valueTypes: {
                string: 'un texte entre guillemets',
                integer: 'un nombre entier',
                number: 'un nombre',
                boolean: 'true ou false',
                array: 'une liste entre crochets [ ]',
                object: 'un objet entre accolades { }',
            },
            // One message per PackProblemCode, with its parameters between braces. The bounds
            // of a length or a count come as one or both of min and max.
            problems: {
                PackJsonInvalid:
                    'JSON mal formé, ligne {line}, colonne {column} : vérifiez les virgules, guillemets et accolades autour.',
                PackPropertyMissing: 'Propriété obligatoire absente : {property}',
                PackPropertyUnknown: 'Propriété inconnue : {property} (faute de frappe ?)',
                PackValueTypeInvalid: 'Valeur incorrecte : il faut {expected}.',
                PackValueOutOfRange: 'Valeur hors limites : de {min} à {max}',
                PackTextLengthOutOfRange: {
                    between: 'Longueur du texte incorrecte : de {min} à {max} caractères',
                    atLeast: 'Texte trop court : longueur minimale {min}',
                    atMost: 'Texte trop long : longueur maximale {max}',
                },
                PackItemCountOutOfRange: {
                    between: 'Nombre d’éléments incorrect : de {min} à {max}',
                    atLeast: 'Pas assez d’éléments : au moins {min}',
                    atMost: 'Trop d’éléments : au plus {max}',
                },
                PackRoundTypeUnknown: 'Type d’activité inconnu : « {type} »',
                PackMediaPathInvalid:
                    'Chemin de média mal écrit : {media} (séparez les dossiers par /, sans caractère spécial)',
                PackMediaOutsidePack: 'Média hors du dossier du pack : {media}',
                PackMediaTypeUnsupported: 'Format de média non pris en charge : {media}',
                PackMediaMissing: 'Média introuvable : {media}',
                PackMediaCaseMismatch:
                    'Majuscules et minuscules différentes du fichier : {media} au lieu de {actual}',
                PackLoadFailed:
                    'Le pack n’a pas pu être chargé à cause d’une erreur inattendue, détaillée dans le journal du serveur.',
                QuizCorrectChoiceMissing:
                    'Question sans bonne réponse : marquez une proposition avec "correct": true',
                QuizCorrectChoiceDuplicated:
                    'Question avec plusieurs bonnes réponses : une seule proposition doit avoir "correct": true',
                QuizChoiceDuplicated: 'Proposition en double : « {choice} »',
            },
        },
        start: {
            action: 'Lancer la partie',
            // Shown while no pack is chosen.
            packRequired: 'Choisissez un pack',
            // Shown while too few players are registered. The minimum is at least 1, never 0.
            minimumPlayers: {
                zero: 'La partie peut être lancée sans joueur.',
                one: 'Il faut au moins {count} joueur inscrit pour lancer la partie.',
                other: 'Il faut au moins {count} joueurs inscrits pour lancer la partie.',
            },
            confirmTitle: 'Lancer la partie ?',
            confirmMessage: {
                zero: 'La partie commencera sans joueur. Les retardataires pourront encore rejoindre.',
                one: 'La partie commencera avec {count} joueur. Les retardataires pourront encore rejoindre.',
                other: 'La partie commencera avec {count} joueurs. Les retardataires pourront encore rejoindre.',
            },
            confirm: 'Lancer',
            cancel: 'Annuler',
            failed: 'Le lancement n’a pas abouti : réessayez.',
        },
        // Shown during the game when the console has nothing more precise to show.
        inProgress: 'Partie en cours',
        nextRound: {
            action: 'Lancer la manche suivante',
            hint: 'La manche suivante démarre sur tous les écrans dès que vous la lancez.',
        },
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
    // The texts of each game mode, under the type of its rounds.
    modes: {
        quiz: {
            question: 'Question {number}/{count}',
            // Read by screen readers: the choices of the question, each with its letter.
            choicesLabel: 'Propositions',
            display: {
                // Read by screen readers: the packs describe no image.
                imageLabel: 'Illustration de la question',
            },
            player: {
                // Read by screen readers: the buttons show a shape and a letter only.
                choiceLabel: 'Proposition {letter}',
            },
            gm: {
                correct: 'Bonne réponse',
                openAnswers: 'Ouvrir les réponses',
                skipQuestion: 'Passer la question',
            },
        },
    },
} as const;
