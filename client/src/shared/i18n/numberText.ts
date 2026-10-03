const french = new Intl.NumberFormat('fr-FR', { maximumFractionDigits: 0 });

/**
 * Writes a whole number the French way, its digits grouped by three (« 1 350 »), with the narrow
 * no-break space Intl uses: a score never breaks across two lines.
 */
export function formatNumber(value: number): string {
    return french.format(value);
}
