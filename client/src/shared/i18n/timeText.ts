const french = new Intl.DateTimeFormat('fr-FR', {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
});

/**
 * Writes the time of day of an instant the French way (« 21:04:05 »), on the clock of the device.
 * The instant is in milliseconds since the Unix epoch, as the server sends it.
 */
export function formatTime(instant: number): string {
    return french.format(new Date(instant));
}
