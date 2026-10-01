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
    },
    gm: {
        waiting: 'Console du game master : en attente de la partie.',
    },
} as const;
