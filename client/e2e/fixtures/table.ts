import { spawn, type ChildProcess } from 'node:child_process';
import { mkdtempSync, rmSync, writeFileSync } from 'node:fs';
import { createServer, type AddressInfo } from 'node:net';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import {
    test as base,
    devices,
    expect,
    type Browser,
    type BrowserContextOptions,
    type Page,
} from '@playwright/test';
import { gameMasterCodeKey } from '../../src/shared/connection/codeStorage.ts';
import { fr } from '../../src/shared/i18n/fr.ts';
import { advertisedAddress, gameMasterCode, packDirectory } from '../gameServer.ts';
import { joinOnNewPhone } from '../players.ts';

const serverDirectory = fileURLToPath(new URL('../../../src/PartyGame.Server', import.meta.url));
// Built by the `dotnet run` of the shared server, which playwright.config.ts starts before any test.
const serverAssembly = join(serverDirectory, 'bin/Debug/net10.0/PartyGame.Server.dll');
const startTimeout = 30_000;

/** A .NET server of a test's own, serving the built front itself. */
export interface DedicatedServer {
    readonly url: string;
    readonly dataDirectory: string;
    /** Kills the process, as a crash would: nothing is saved nor closed. */
    kill(): Promise<void>;
    /** Starts the server again, on the same port and data folder. */
    start(): Promise<void>;
}

interface TablePlayer {
    readonly nickname: string;
    readonly page: Page;
}

/**
 * The TV screen, the game master console and three players registered in the lobby: Zoé on an
 * iPhone under WebKit, Max and Léa on Pixels under Chromium. Each has a browser context of its own.
 */
export interface Table {
    readonly display: Page;
    readonly gm: Page;
    readonly players: readonly [TablePlayer, TablePlayer, TablePlayer];
}

async function freePort(): Promise<number> {
    const server = createServer();
    await new Promise<void>((resolve) => server.listen(0, resolve));
    const { port } = server.address() as AddressInfo;
    await new Promise((resolve) => server.close(resolve));
    return port;
}

async function newPage(
    browser: Browser,
    device: BrowserContextOptions,
    baseURL: string,
): Promise<Page> {
    return (await browser.newContext({ ...device, baseURL })).newPage();
}

export const test = base.extend<
    { dedicatedServer: DedicatedServer; table: Table },
    { webkitBrowser: Browser }
>({
    // Playwright reads the fixtures a fixture needs from its destructured first argument.
    // eslint-disable-next-line no-empty-pattern
    dedicatedServer: async ({}, use, testInfo) => {
        const port = await freePort();
        const root = mkdtempSync(join(tmpdir(), 'partygame-e2e-dedicated-'));
        const dataDirectory = join(root, 'data');
        let log = '';
        let child: ChildProcess | undefined;

        const kill = async () => {
            const running = child;
            child = undefined;
            if (running && running.exitCode === null) {
                const exited = new Promise((resolve) => running.once('exit', resolve));
                running.kill('SIGKILL');
                await exited;
            }
        };
        const start = async () => {
            const started = spawn(
                'dotnet',
                [
                    serverAssembly,
                    `--Network:Port=${port}`,
                    `--Network:AdvertisedAddress=${advertisedAddress}`,
                    `--GameMaster:Code=${gameMasterCode}`,
                    `--Packs:Directory=${packDirectory}`,
                    `--Persistence:Directory=${dataDirectory}`,
                    // Out of the shared logs folder, whose file the shared server holds.
                    `--LogFiles:Directory=${join(root, 'logs')}`,
                ],
                // The content root, holding the web root `npm run build` wrote.
                { cwd: serverDirectory },
            );
            child = started;
            started.stdout.on('data', (chunk: Buffer) => (log += chunk.toString()));
            started.stderr.on('data', (chunk: Buffer) => (log += chunk.toString()));
            const deadline = Date.now() + startTimeout;
            while (Date.now() < deadline && started.exitCode === null) {
                const healthy = await fetch(`http://localhost:${port}/health`).then(
                    (response) => response.ok,
                    () => false,
                );
                if (healthy) {
                    return;
                }
                await new Promise((resolve) => setTimeout(resolve, 200));
            }
            throw new Error(`The dedicated server did not start on port ${port}:\n${log}`);
        };

        try {
            await start();
            await use({ url: `http://localhost:${port}`, dataDirectory, kill, start });
        } finally {
            await kill();
            if (testInfo.status !== testInfo.expectedStatus) {
                const path = testInfo.outputPath('server.log');
                writeFileSync(path, log);
                await testInfo.attach('server.log', { path, contentType: 'text/plain' });
            }
            rmSync(root, { recursive: true, force: true });
        }
    },

    // Every page of the test opens on its dedicated server.
    baseURL: async ({ dedicatedServer }, use) => {
        await use(dedicatedServer.url);
    },

    webkitBrowser: [
        async ({ playwright }, use) => {
            const browser = await playwright.webkit.launch();
            await use(browser);
            await browser.close();
        },
        { scope: 'worker' },
    ],

    // `browser` is the Chromium of the desktop-chrome project, the only one running these tests.
    table: async ({ dedicatedServer, browser, webkitBrowser }, use, testInfo) => {
        const { url } = dedicatedServer;
        const display = await newPage(browser, devices['Desktop Chrome'], url);
        await display.goto('/display/');
        const gm = await newPage(browser, devices['Desktop Chrome'], url);
        await gm.addInitScript(
            ([key, code]) => {
                localStorage.setItem(key, code);
            },
            [gameMasterCodeKey, gameMasterCode] as const,
        );
        await gm.goto('/gm/');
        await expect(gm.getByRole('heading', { name: fr.gm.consoleTitle })).toBeVisible();
        const players: Table['players'] = [
            {
                nickname: 'Zoé',
                page: await joinOnNewPhone(webkitBrowser, url, 'Zoé', devices['iPhone 15']),
            },
            {
                nickname: 'Max',
                page: await joinOnNewPhone(browser, url, 'Max', devices['Pixel 7']),
            },
            {
                nickname: 'Léa',
                page: await joinOnNewPhone(browser, url, 'Léa', devices['Pixel 7']),
            },
        ];
        const pages = {
            display,
            gm,
            ...Object.fromEntries(players.map((p) => [p.nickname, p.page])),
        };

        await use({ display, gm, players });

        for (const [name, page] of Object.entries(pages)) {
            if (testInfo.status !== testInfo.expectedStatus && !page.isClosed()) {
                // Saved as a file, so that the list reporter prints where to find it.
                const path = testInfo.outputPath(`${name}.png`);
                await page.screenshot({ path });
                await testInfo.attach(name, { path, contentType: 'image/png' });
            }
            await page.context().close();
        }
    },
});

export { expect };
