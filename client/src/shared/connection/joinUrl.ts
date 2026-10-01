/**
 * Builds the player page URL phones open from the QR code. The port is the one the TV page was
 * loaded from, not the .NET server's: behind the Vite dev server, phones must reach Vite too.
 */
export function composeJoinUrl(address: string, page: Pick<Location, 'protocol' | 'port'>): string {
    const port = page.port === '' ? '' : `:${page.port}`;
    return `${page.protocol}//${address}${port}/`;
}
