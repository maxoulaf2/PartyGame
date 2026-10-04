import { existsSync } from 'node:fs';
import { join } from 'node:path';
import { gameMasterCodeKey } from '../src/shared/connection/codeStorage.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { expect, test } from './fixtures/table.ts';
import { gameMasterCode } from './gameServer.ts';
import { joinOnNewPhone } from './players.ts';

test('a dedicated server killed then started again finds the game it saved', async ({
    dedicatedServer,
    page,
    browser,
    baseURL,
}) => {
    await joinOnNewPhone(browser, baseURL, 'Zoé');
    await expect
        .poll(() => existsSync(join(dedicatedServer.dataDirectory, 'current-game.json')))
        .toBe(true);

    await dedicatedServer.kill();
    await expect(fetch(`${dedicatedServer.url}/health`)).rejects.toThrow();
    await dedicatedServer.start();

    // Same port, same data folder: the console is offered to resume the saved lobby.
    await page.addInitScript(
        ([key, code]) => {
            localStorage.setItem(key, code);
        },
        [gameMasterCodeKey, gameMasterCode] as const,
    );
    await page.goto('/gm/');
    await expect(page.getByRole('heading', { name: fr.gm.resume.title })).toBeVisible();
});
