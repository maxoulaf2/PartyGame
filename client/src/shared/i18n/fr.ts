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
        // The ranking shown between two rounds, written by rankText and standingText.
        rankingAfter: 'Classement après la manche {number}',
        // The ranking shown once the last round is over.
        finalRanking: 'Classement final',
        rank: {
            first: '{rank}er',
            other: '{rank}e',
        },
        standing: {
            alone: '{rank} sur {count}',
            tied: '{rank} ex aequo sur {count}',
        },
        // Read by screen readers: the ranked players.
        rankingLabel: 'Classement',
        // Read by screen readers on the TV screen: the steps of the final ranking, then the others.
        podiumLabel: 'Podium',
        // The players who share a step of the podium, `{rank}` written by rankText.
        podiumStepLabel: 'Sur la marche {rank}',
        restLabel: 'Suite du classement',
        // The points of a player since the start of the game, in a ranking.
        points: {
            zero: '0 point',
            one: '{count} point',
            other: '{count} points',
        },
    },
    // The buzzer of a phone, by state (see BuzzerState).
    buzzer: {
        closed: 'Attends la question',
        open: 'Buzz !',
        sent: 'Buzz envoyé…',
        won: 'À toi de répondre !',
        lost: 'Un autre joueur a la main',
        blocked: 'Tu ne peux plus buzzer sur cette question',
    },
    player: {
        waiting: 'Bienvenue ! La partie va bientôt commencer.',
        // Shown during the game when the phone has nothing more precise to show.
        inProgress: 'La partie est en cours : garde un œil sur l’écran !',
        betweenRounds: 'La suite arrive bientôt : garde un œil sur l’écran !',
        finished: 'Merci d’avoir joué !',
        // Once the game is finished, under the final rank of a player on the podium.
        podium: 'Bravo, tu es sur le podium !',
        // Shown to a phone that joined once the game was finished: it has no rank.
        joinedAfterEnd: 'Cette partie vient de se terminer : rendez-vous à la prochaine !',
        registeredAs: 'Tu es inscrit sous le nom {nickname}',
        // Shown while a phone that joined before waits to be recognized by the server.
        resuming: 'Retour dans la partie…',
        // Shown instead of the form while the server, restarted, waits for the game master to
        // resume the game or start a new one.
        gamePending: 'La partie va reprendre dans un instant…',
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
        // Shown while the server, restarted, waits for the game master to resume the game.
        resumePending: 'Reprise de la partie…',
        // Clicked once on the TV screen to let its browser play sound.
        startAudio: 'Démarrer',
        // Shown instead of what the TV screen could not render, until the next update.
        continuing: 'La partie continue…',
        // Shown next to the QR code between two rounds, for late arrivals.
        lateArrivals: 'Pas encore inscrit ? Scannez pour rejoindre',
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
            expired:
                'Le serveur a redémarré avec un nouveau code : relisez-le dans sa console, puis saisissez-le.',
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
        // The points of a player since the start of the game, next to their nickname.
        score: {
            zero: '0 point',
            one: '{count} point',
            other: '{count} points',
        },
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
        network: {
            title: 'Diagnostic réseau',
            hint: 'Avant l’arrivée des invités, ouvrez cette page sur un ou deux téléphones : elle vérifie en une vingtaine de secondes qu’ils communiquent bien avec le serveur, sans les inscrire.',
            // Read by screen readers: the QR code of the diagnostic page.
            qrLabel: 'QR code de la page de diagnostic',
            none: 'Aucune adresse de réseau local : la page de diagnostic n’a pas d’adresse à donner.',
            diagnosticsLabel: 'Derniers diagnostics, du plus récent au plus ancien',
            noDiagnostic: 'Aucun diagnostic pour l’instant.',
            // A diagnostic: the device, when it ran, its verdict and its median round trip.
            diagnostic: '{device} à {time} : {verdict}',
            // The kind of device, deduced by the server from the browser.
            devices: {
                IPhone: 'iPhone',
                IPad: 'iPad',
                Android: 'Android',
                Windows: 'PC Windows',
                Mac: 'Mac',
                Linux: 'Linux',
                Other: 'Appareil',
            },
            verdicts: {
                Good: 'tout est bon',
                Reserved: 'utilisable, avec des réserves',
                Problem: 'problème',
            },
            // The connection of a player or of the TV screen, in the list of the players.
            display: 'Écran TV',
            // Next to the TV screen, until someone clicks « Démarrer » on it.
            displayAudioLocked: 'Son de la TV non activé',
            roundTrip: '{value} ms',
            noRoundTrip: '— ms',
            // How far the clock of the device may be from the server: half its round trip.
            clockUncertainty: 'horloge ±{value} ms',
            noClockUncertainty: 'horloge ± — ms',
            transports: {
                WebSockets: 'WebSocket',
                ServerSentEvents: 'connexion de repli (SSE)',
                LongPolling: 'connexion de repli (polling)',
            },
            reconnections: {
                zero: '0 reconnexion',
                one: '{count} reconnexion',
                other: '{count} reconnexions',
            },
            // Marks a poor measure, with the color: never the color alone.
            warning: '⚠',
            // Read by screen readers: what the mark means.
            warningLabel: 'Valeur mauvaise',
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
                image: 'une image .jpg, .jpeg, .png ou .webp',
                audio: 'un fichier .mp3',
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
                PackMediaTypeUnsupported:
                    'Format de média non pris en charge : {media} (il faut {expected})',
                PackMediaMissing: 'Média introuvable : {media}',
                PackMediaCaseMismatch:
                    'Majuscules et minuscules différentes du fichier : {media} au lieu de {actual}',
                PackMediaUnreadable: 'Fichier MP3 illisible : {media}',
                PackAudioExcerptStartBeyondEnd:
                    'L’extrait commence à {start} s, après la fin du morceau ({duration} s)',
                PackLoadFailed:
                    'Le pack n’a pas pu être chargé à cause d’une erreur inattendue, détaillée dans le journal du serveur.',
                QuizCorrectChoiceMissing:
                    'Question sans bonne réponse : marquez une proposition avec "correct": true',
                QuizCorrectChoiceDuplicated:
                    'Question avec plusieurs bonnes réponses : une seule proposition doit avoir "correct": true',
                QuizChoiceDuplicated: 'Proposition en double : « {choice} »',
                BlindTestPointsMissing:
                    'Aucun morceau ne rapporte de points : donnez des points au titre, ou à l’artiste d’au moins un morceau',
                OpenQuestionAnswerLengthOutOfRange:
                    'Réponse vide ou trop longue : de 1 à {max} caractères (maxLength de la manche)',
                OpenQuestionAnswerEmpty:
                    'Réponse « {answer} » vide une fois la casse, les accents, la ponctuation et l’article initial ignorés',
                OpenQuestionAnswerDuplicated:
                    'Variante « {answer} » en double : elle revient à la réponse attendue ou à une autre variante',
                OpenQuestionAnswerNotNumeric:
                    'Réponse « {answer} » non numérique : une question "numeric" n’accepte que des chiffres',
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
        // Offered once the server restarted and found the game it saved before.
        resume: {
            title: 'Partie interrompue',
            intro: 'Le serveur a redémarré et a retrouvé la partie enregistrée. Que voulez-vous faire ?',
            packLabel: 'Pack',
            // The game found was still in its lobby, no pack chosen.
            noPack: 'Aucun pack choisi',
            progressLabel: 'Avancement',
            playersLabel: 'Joueurs',
            savedAtLabel: 'Enregistrée à',
            // Where the game found stopped, the parts joined by « · ».
            progress: {
                lobby: 'Lobby',
                round: 'Manche {number}/{count} · {title}',
                roundEnded: 'Fin de la manche {number}/{count} · {title}',
                step: 'Question {number}/{count}',
                finished: 'Partie terminée',
            },
            players: {
                zero: 'Aucun joueur',
                one: '{count} joueur inscrit',
                other: '{count} joueurs inscrits',
            },
            missingMedia: {
                zero: 'Tous les médias du pack sont présents.',
                one: 'Un média du pack est introuvable : remettez-le à sa place pour pouvoir reprendre la partie.',
                other: '{count} médias du pack sont introuvables : remettez-les à leur place pour pouvoir reprendre la partie.',
            },
            missingMediaLabel: 'Médias introuvables',
            checkAgain: 'Vérifier de nouveau',
            resume: 'Reprendre la partie',
            newGame: 'Nouvelle partie',
            confirmTitle: 'Commencer une nouvelle partie ?',
            confirmMessage:
                'La partie interrompue ne sera pas reprise : les joueurs devront se réinscrire. Elle reste enregistrée dans le fichier previous-game.json du dossier des données.',
            confirm: 'Nouvelle partie',
            cancel: 'Annuler',
            failed: 'La demande n’a pas abouti : réessayez.',
        },
        // Shown instead of the view of the round the console could not render: the rest of the
        // console still works, and the view comes back with the next update.
        roundViewUnavailable: 'Affichage de la manche indisponible',
        // Shown instead of the whole console when it could not render, until the next update.
        consoleUnavailable: 'Console momentanément indisponible : elle revient d’elle-même.',
        nextRound: {
            // The round the game master starts next, with its title from the pack.
            upcoming: 'Manche suivante ({number}/{count}) : {title}',
            action: 'Lancer la manche suivante',
            hint: 'La manche suivante démarre sur tous les écrans dès que vous la lancez.',
        },
        // Offered while the round in progress keeps failing: the server could not handle several
        // of its actions. Players and TV screen see the usual end of round, nothing about it.
        // Outside the lobby, which always shows it: in a corner of the TV screen, over the game.
        joinCode: {
            show: 'Afficher le QR code sur la TV',
            hide: 'Masquer le QR code de la TV',
        },
        skipRound: {
            problem: 'Cette manche rencontre un problème. Passer la manche ?',
            hint: 'Les points déjà gagnés sont conservés, ceux de la question en cours ne sont pas attribués.',
            action: 'Passer la manche',
            confirmTitle: 'Passer la manche ?',
            confirmMessage:
                'La manche se termine tout de suite, sur tous les écrans. Les points déjà gagnés sont conservés, ceux de la question en cours ne sont pas attribués.',
            confirm: 'Passer la manche',
            cancel: 'Annuler',
            failed: 'La manche n’a pas pu être passée : réessayez.',
            // Between two rounds or once the game is finished, on the console alone.
            skipped: 'Manche passée : les points de la question en cours n’ont pas été attribués.',
        },
        connected: 'Connecté',
        disconnected: 'Déconnecté',
        // Failures the server recovered from by itself: the game master alone hears of them.
        incidents: {
            // The discreet counter of the header, for the incidents not read yet. Once they are
            // read, « zero » still opens the list.
            counter: {
                zero: 'Incidents',
                one: '{count} incident',
                other: '{count} incidents',
            },
            title: 'Incidents du serveur',
            hint: 'La partie continue : aucun message d’erreur n’est apparu sur les téléphones ni sur l’écran TV.',
            listLabel: 'Incidents, du plus récent au plus ancien',
            markAllRead: 'Tout marquer comme lu',
            close: 'Fermer',
            // When it happened, written by formatTime; then, for a repeated one, how many times.
            at: 'À {time}',
            repeated: '{count} fois, la dernière à {time}',
            // The round in progress when it happened, and the step of the round concerned, if any:
            // a question of a quiz.
            round: 'Manche {number}/{count} : {title}',
            roundStep: 'Manche {number}/{count} : {title}, question {step}',
            outsideRound: 'Hors manche',
            // One message per IncidentCode, {role} taken from `roles`.
            codes: {
                RoundHandlerFailed: 'Une action n’a pas pu être traitée : elle a été ignorée.',
                ProjectionFailed:
                    'L’affichage {role} n’a pas pu être mis à jour : il garde l’état précédent.',
                EffectFailed: 'Une opération a échoué après une action : la partie continue.',
                DisplayViewFailed:
                    'L’écran TV n’a pas pu afficher la manche : il montre un écran d’attente jusqu’à la prochaine mise à jour.',
                DisplayMediaFailed:
                    'L’écran TV n’a pas pu charger une image ou un son : le public n’en voit rien.',
                SavedGameUnreadable:
                    'La partie enregistrée avant le redémarrage du serveur n’a pas pu être relue : une nouvelle partie a commencé et les joueurs doivent se réinscrire. Le fichier a été mis de côté dans le dossier des données.',
                PersistenceFailed:
                    'La partie n’a pas pu être enregistrée sur le disque : elle continue, mais ne pourrait pas reprendre après un arrêt du serveur. Ce message disparaît dès que l’enregistrement fonctionne de nouveau.',
            },
            roles: {
                Player: 'des téléphones',
                Display: 'de l’écran TV',
                GameMaster: 'de la console',
            },
        },
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
    // The page /diagnostic/, opened on a phone by the game master on arrival at the venue.
    diagnostic: {
        title: 'Diagnostic réseau',
        intro: 'Ce test vérifie en une vingtaine de secondes que ce téléphone communique bien avec le serveur. Gardez la page ouverte et l’écran allumé.',
        steps: {
            connecting: 'Connexion au serveur…',
            roundTrips: 'Mesure du temps de réponse…',
            stability: 'Vérification de la stabilité ({seconds} s)…',
            download: 'Mesure du débit…',
        },
        // The verdict, with a symbol: never the color alone.
        verdicts: {
            Good: '✓ Tout est bon',
            Reserved: '⚠ Utilisable, avec des réserves',
            Problem: '✕ Problème',
        },
        // One advice per measure in default.
        advice: {
            unreachable:
                'Le téléphone n’arrive pas à joindre le serveur : vérifiez qu’il est connecté au même Wi-Fi que le serveur, et que le serveur est bien lancé.',
            roundTripHigh:
                'Le serveur répond lentement : le buzzer pourrait être moins précis. Rapprochez-vous du point d’accès Wi-Fi.',
            roundTripTooHigh:
                'Le serveur répond bien trop lentement pour jouer : rapprochez-vous du point d’accès Wi-Fi, ou essayez un autre réseau.',
            losses: 'Des messages se perdent : le Wi-Fi est instable à cet endroit. Rapprochez-vous du point d’accès.',
            reconnections:
                'La connexion a été coupée pendant le test : le Wi-Fi est instable, ou le téléphone a changé de réseau.',
            fallbackTransport:
                'La connexion passe par un mode de repli plus lent : un pare-feu ou un proxy bloque peut-être les WebSockets.',
            otherNetwork:
                'Ce téléphone passe par un autre réseau que le serveur : réseau invité ? Connectez-le au même Wi-Fi que le serveur.',
            slowDownload: 'Le débit est faible : les images pourraient s’afficher lentement.',
            downloadFailed:
                'Le téléchargement de test a échoué : les images pourraient ne pas s’afficher.',
        },
        measuresLabel: 'Mesures',
        transport: 'Connexion',
        transports: {
            WebSockets: 'WebSocket',
            ServerSentEvents: 'Mode de repli (SSE)',
            LongPolling: 'Mode de repli (polling)',
        },
        unreachable: 'Serveur injoignable',
        roundTrip: 'Temps de réponse',
        roundTripValue: 'médiane {median} ms · max {max} ms · gigue {jitter} ms',
        stability: 'Stabilité',
        lost: {
            zero: 'aucune perte',
            one: '{count} perte',
            other: '{count} pertes',
        },
        stabilityValue: '{lost} sur {pings} · {reconnections}',
        reconnections: {
            zero: 'aucune coupure',
            one: '{count} coupure',
            other: '{count} coupures',
        },
        throughput: 'Débit',
        throughputValue: '{value} Mbit/s',
        network: 'Réseau',
        networks: {
            same: 'Même réseau que le serveur',
            other: 'Autre réseau que le serveur',
            unknown: 'Non déterminé',
        },
        notMeasured: 'Non mesuré',
        retry: 'Relancer le test',
        // The clock of the phone, estimated from the server: what the buzzer relies on.
        clock: {
            title: 'Horloge',
            syncing: 'Synchronisation de l’horloge avec le serveur…',
            failed: 'L’horloge n’a pas pu être synchronisée avec le serveur : le flash synchronisé n’est pas disponible. Relancez le test.',
            value: 'aller-retour {roundTrip} ms · incertitude ±{uncertainty} ms',
        },
        flash: {
            start: 'Flash synchronisé',
            hint: 'Posez plusieurs téléphones côte à côte et lancez le flash sur chacun : ils clignotent ensemble à chaque seconde si leurs horloges sont bien alignées.',
            stop: 'Touchez l’écran pour arrêter',
        },
    },
    // The texts of each game mode, under the type of its rounds.
    modes: {
        quiz: {
            question: 'Question {number}/{count}',
            // Read by screen readers: the choices of the question, each with its letter.
            choicesLabel: 'Propositions',
            // Shown once the countdown locked the answers.
            timeUp: 'Temps écoulé',
            // Shown instead of « Temps écoulé » once everybody answered, which locks the answers early.
            allAnswered: 'Tous les joueurs ont répondu',
            // Read by screen readers before the seconds left to answer.
            timeLeft: 'Temps restant',
            // How many of the players taking part answered, never what.
            answered: 'Réponses : {answered} / {participants}',
            // Marks the correct choice, next to an icon: never told by the color alone.
            correct: 'Bonne réponse',
            // A player taking part who did not choose before the answers were locked.
            noAnswer: 'Pas de réponse',
            // The points a player earned with the question revealed, 0 included, written by formatNumber.
            pointsEarned: '+{points}',
            // How many players chose a choice.
            choiceAnswers: {
                zero: 'aucune réponse',
                one: '{count} réponse',
                other: '{count} réponses',
            },
            display: {
                // Shown large while the game master reads out the question, before showing it.
                upcoming: 'Question {number}',
                // Under it, while the buzzer is open and the game master reads the question aloud.
                listen: 'Buzzers ouverts : écoutez bien !',
                // Read by screen readers: the packs describe no image.
                imageLabel: 'Illustration de la question',
                // Read by screen readers: the players who chose a choice, once revealed.
                choicePlayersLabel: 'Joueurs ayant choisi {letter}',
                // Heads the players taking part who did not answer, once revealed.
                unanswered: 'Sans réponse',
            },
            player: {
                // Read by screen readers: the buttons show a shape and a letter only.
                choiceLabel: 'Proposition {letter}',
                // The choice is sent, not confirmed by the server yet.
                pending: 'Envoi de ta réponse…',
                recorded: 'Réponse enregistrée',
                // For a player who joined once the answers were open.
                nextQuestion: 'Tu joueras à la question suivante',
                // What the reveal tells the player, one per QuizVerdict.
                verdicts: {
                    Correct: 'Bonne réponse !',
                    Wrong: 'Raté',
                    NoAnswer: 'Pas de réponse',
                },
                // Heads the correct choice once revealed, unless the player chose it.
                correctChoice: 'La bonne réponse',
                // The points of the player since the start of the game, under those of the question.
                score: {
                    zero: 'Total : 0 point',
                    one: 'Total : {count} point',
                    other: 'Total : {count} points',
                },
            },
            gm: {
                // The game master shows on the TV screen what they just read out: the question, then
                // each choice in turn, which the players may choose at once. The last one starts the
                // countdown.
                showQuestion: 'Afficher la question',
                showChoice: 'Afficher la proposition {letter}',
                // Marks the question, or a choice, the TV screen does not show yet.
                hiddenOnDisplay: 'Pas encore affichée sur la TV',
                revealAnswer: 'Révéler',
                skipQuestion: 'Passer la question',
                // Asked before skipping: the answers received and the question are lost.
                skipConfirm: {
                    title: 'Passer la question {number} ?',
                    message:
                        'Les réponses reçues seront ignorées et personne ne marquera de point sur cette question. La question suivante s’affichera sur tous les écrans.',
                    // When the question skipped is the last one of the round.
                    lastMessage:
                        'Les réponses reçues seront ignorées et personne ne marquera de point sur cette question. C’est la dernière question : la manche se terminera.',
                    confirm: 'Passer',
                    cancel: 'Annuler',
                },
                nextQuestion: 'Question suivante',
                // Replaces « Question suivante » once the last question of the round is revealed.
                endRound: 'Terminer la manche',
                answersLabel: 'Réponses des joueurs',
                // A player taking part who has not chosen while the answers are open.
                waitingAnswer: 'Pas encore de réponse',
            },
        },
        buzzer: {
            question: 'Question {number}/{count}',
            // The player who has the hand, on every screen but their own phone.
            hasHand: '{nickname} a la main',
            // Once revealed, the player whose answer the game master judged correct.
            foundBy: '{nickname} a trouvé !',
            // Once revealed, when nobody found.
            nobodyFound: 'Personne n’a trouvé',
            display: {
                // Shown large until the game master asks the question.
                upcoming: 'Question {number}',
                // Under it, while the buzzer is open and the game master reads the question aloud.
                listen: 'Buzzers ouverts : écoutez bien !',
                // Read by screen readers: the packs describe no image.
                imageLabel: 'Illustration de la question',
                // Heads the expected answer once revealed.
                answer: 'Réponse : {answer}',
            },
            player: {
                // The points the player earned with the question revealed, 0 included, written by formatNumber.
                pointsEarned: '+{points}',
                // The points of the player since the start of the game, under those of the question.
                score: {
                    zero: 'Total : 0 point',
                    one: 'Total : {count} point',
                    other: 'Total : {count} points',
                },
            },
            gm: {
                // Shows the question on the TV screen and opens the buzzer on every phone.
                askQuestion: 'Poser la question',
                // Opens the buzzer while the TV screen keeps the question hidden, for it to be read aloud.
                openHidden: 'Ouvrir le buzzer sans afficher',
                // Shows on the TV screen the question asked hidden.
                showQuestion: 'Afficher la question',
                // Marks the question the TV screen does not show yet.
                hiddenOnDisplay: 'Pas encore affichée sur la TV',
                answer: 'Réponse attendue : {answer}',
                waitingBuzz: 'Buzzer ouvert : en attente d’un buzz',
                // The game master judges the answer the player who has the hand gave out loud.
                correct: 'Bonne réponse',
                wrong: 'Mauvaise réponse',
                // Once every connected player is blocked: only the reveal remains.
                allBlocked: 'Tous les joueurs sont bloqués',
                // Reveals the answer without points, whoever has the hand.
                revealAnswer: 'Révéler la réponse',
                nextQuestion: 'Question suivante',
                // Replaces « Question suivante » once the last question of the round is revealed.
                endRound: 'Terminer la manche',
            },
        },
        blindtest: {
            track: 'Extrait {number} / {count}',
            // The player who has the hand, on every screen but their own phone.
            hasHand: '{nickname} a la main',
            // What was found already, on the TV screen and the console: never the answer itself.
            titleFoundBy: 'Titre trouvé par {nickname}',
            artistFoundBy: 'Artiste trouvé par {nickname}',
            // The buzzer of a phone, where it differs from the questions (see fr.buzzer).
            buzzer: {
                closed: 'Attends la musique',
                blocked: 'Bloqué pour cet extrait',
            },
            // On the phone of a player once judged, without the points until the reveal.
            found: {
                title: 'Tu as trouvé le titre !',
                artist: 'Tu as trouvé l’artiste !',
                both: 'Tu as trouvé le titre et l’artiste !',
            },
            // Once revealed, when nobody found anything.
            nobodyFound: 'Personne n’a trouvé',
            display: {
                // Under the number of the track, until the game master plays it.
                ready: 'Prêts ?',
                // Under it, while the music plays.
                listen: 'Buzzers ouverts : écoutez bien !',
                // Under the title revealed.
                artist: 'par {artist}',
                // Read by screen readers: the packs describe no image.
                imageLabel: 'Visuel du morceau',
            },
            player: {
                // The points the player earned with the track revealed, 0 included, written by formatNumber.
                pointsEarned: '+{points}',
                // The points of the player since the start of the game, under those of the track.
                score: {
                    zero: 'Total : 0 point',
                    one: 'Total : {count} point',
                    other: 'Total : {count} points',
                },
            },
            gm: {
                // Before the title and the artist, which only the console shows.
                title: 'Titre : {title}',
                artist: 'Artiste : {artist}',
                // For a track played on its title only.
                noArtist: 'Pas d’artiste à trouver',
                // Plays the excerpt on the TV screen and opens the buzzer on every phone.
                play: 'Lancer l’extrait',
                waitingBuzz: 'Musique en cours : en attente d’un buzz',
                // The judgment of the answer given out loud, ticked then sent with « Valider ».
                judge: 'Réponse du joueur',
                titleFound: 'Titre trouvé',
                artistFound: 'Artiste trouvé',
                nothingFound: 'Rien de bon',
                validate: 'Valider',
                // Stops the music and shows the track on the TV screen, whoever has the hand.
                revealAnswer: 'Révéler la réponse',
                nextTrack: 'Extrait suivant',
                // Replaces « Extrait suivant » once the last track of the round is revealed.
                endRound: 'Terminer la manche',
                skipTrack: 'Passer l’extrait',
                // Asked before skipping: the track earns nobody any point.
                skipConfirm: {
                    title: 'Passer l’extrait {number} ?',
                    message:
                        'Personne ne marquera de point sur ce morceau. L’extrait suivant s’annoncera sur tous les écrans.',
                    // When the track skipped is the last one of the round.
                    lastMessage:
                        'Personne ne marquera de point sur ce morceau. C’est le dernier extrait : la manche se terminera.',
                    confirm: 'Passer',
                    cancel: 'Annuler',
                },
            },
        },
        openquestion: {
            question: 'Question {number}/{count}',
            // Shown once the countdown locked the answers.
            timeUp: 'Temps écoulé',
            // Shown instead of « Temps écoulé » once everybody answered, which locks the answers early.
            allAnswered: 'Tous les joueurs ont répondu',
            // Read by screen readers before the seconds left to answer.
            timeLeft: 'Temps restant',
            // How many of the players taking part answered, never what.
            answered: 'Réponses : {answered} / {participants}',
            display: {
                // Shown large while the game master reads out the question, before showing it.
                upcoming: 'Question {number}',
                // Read by screen readers: the packs describe no image.
                imageLabel: 'Illustration de la question',
            },
            player: {
                // Labels the field the answer is typed in.
                answerLabel: 'Ta réponse',
                // Before the game master shows the question on the TV screen.
                waitQuestion: 'Lis la question sur la TV dès qu’elle s’affiche',
                send: 'Envoyer',
                // The answer is sent, not confirmed by the server yet.
                pending: 'Envoi de ta réponse…',
                recorded: 'Réponse enregistrée',
                // For a player who joined once the answers were open.
                nextQuestion: 'Tu joueras à la question suivante',
            },
            gm: {
                // Shows the question on the TV screen, which opens the answers and starts the countdown.
                showQuestion: 'Afficher la question',
                // Marks the question while the TV screen does not show it yet.
                hiddenOnDisplay: 'Pas encore affichée sur la TV',
                // Heads the expected answer, as the pack writes it.
                expectedAnswer: 'Réponse attendue : {answer}',
                // Heads the other answers the pack accepts.
                acceptedAnswers: 'Aussi acceptées : {answers}',
                answersLabel: 'Réponses des joueurs',
                // A player taking part who has not answered while the answers are open.
                waitingAnswer: 'Pas encore de réponse',
                // A player taking part who did not answer before the answers were locked.
                noAnswer: 'Pas de réponse',
                skipQuestion: 'Passer la question',
                // Asked before skipping: the answers received and the question are lost.
                skipConfirm: {
                    title: 'Passer la question {number} ?',
                    message:
                        'Les réponses reçues seront ignorées et personne ne marquera de point sur cette question. La question suivante s’affichera sur tous les écrans.',
                    // When the question skipped is the last one of the round.
                    lastMessage:
                        'Les réponses reçues seront ignorées et personne ne marquera de point sur cette question. C’est la dernière question : la manche se terminera.',
                    confirm: 'Passer',
                    cancel: 'Annuler',
                },
            },
        },
    },
} as const;
