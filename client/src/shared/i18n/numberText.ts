const french = new Intl.NumberFormat('fr-FR', { maximumFractionDigits: 0 });

/**
 * Writes a whole number the French way, its digits grouped by three (« 1 350 »), with the narrow
 * no-break space Intl uses: a score never breaks across two lines.
 */
export function formatNumber(value: number): string {
    return french.format(value);
}

const tenths = new Intl.NumberFormat('fr-FR', {
    minimumFractionDigits: 1,
    maximumFractionDigits: 1,
});

/** Writes a duration in seconds to the tenth, the French way (« 4,3 »): how fast a player answered. */
export function formatSeconds(milliseconds: number): string {
    return tenths.format(milliseconds / 1000);
}
