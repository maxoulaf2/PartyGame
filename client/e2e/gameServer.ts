import { mkdtempSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';

/**
 * The .NET server the E2E tests start behind the preview server, on a port of its own so that a
 * server left running on the default port does not get in the way.
 */
export const gameServerPort = 5199;

/** Fixed by `GameMaster:Code`: tests type it like a game master reading the server console. */
export const gameMasterCode = '246810';

/**
 * Imposed by `Network:AdvertisedAddress`, since the detected one depends on the machine: the TV
 * screen encodes it in its QR code.
 */
export const advertisedAddress = '192.168.1.42';

/**
 * The packs of the tests, read by the server through `Packs:Directory`: two valid packs, so that
 * none is chosen at startup, and an invalid one.
 */
export const packDirectory = fileURLToPath(new URL('./packs', import.meta.url));

/** The valid pack the tests choose, its title and the titles of its rounds. */
export const playedPack = {
    id: 'soiree',
    title: 'Grande soirée',
    rounds: ['Échauffement', 'Finale'],
} as const;

/**
 * The folder the E2E server saves its game to, through `Persistence:Directory`: empty on each run,
 * so that the game of a previous run is never offered to resume. Drawn by the main process only,
 * which starts the server: the workers load the configuration too.
 */
export const dataDirectory =
    process.env.TEST_WORKER_INDEX === undefined
        ? mkdtempSync(join(tmpdir(), 'partygame-e2e-'))
        : '';
