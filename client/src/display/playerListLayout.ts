/** How the TV screen lays out its list of players. */
export interface PlayerListLayout {
    readonly columns: number;
    /** Font size of the nicknames, relative to the height of the screen. */
    readonly fontSize: string;
}

/**
 * Fewer players get larger nicknames, more players more columns, so that the whole list fits on a
 * 1080p screen without scrolling and stays readable from across the room up to 20 players. Beyond,
 * the list still fits but in smaller type.
 */
export function playerListLayout(count: number): PlayerListLayout {
    if (count <= 6) {
        return { columns: 1, fontSize: '4.4vh' };
    }
    if (count <= 12) {
        return { columns: 2, fontSize: '3.7vh' };
    }
    if (count <= 20) {
        return { columns: 2, fontSize: '3vh' };
    }
    return { columns: 3, fontSize: '2.2vh' };
}
