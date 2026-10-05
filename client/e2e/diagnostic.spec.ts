import { expect, test, type Browser, type Page } from '@playwright/test';
import { gameMasterCodeKey } from '../src/shared/connection/codeStorage.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { gameMasterCode } from './gameServer.ts';
import { trackExternalRequests } from './localRequests.ts';

/** Opens the GM console on a device of its own, with the right code already remembered. */
async function openConsole(browser: Browser, baseURL: string | undefined): Promise<Page> {
    const page = await (await browser.newContext({ baseURL })).newPage();
    await page.addInitScript(
        ([key, code]) => {
            localStorage.setItem(key, code);
        },
        [gameMasterCodeKey, gameMasterCode] as const,
    );
    await page.goto('/gm/');
    await expect(page.getByRole('heading', { name: fr.gm.consoleTitle })).toBeVisible();
    return page;
}

test('a phone checks the network on site, and the GM console lists its result', async ({
    page,
    browser,
    baseURL,
}, testInfo) => {
    // The test itself lasts about 20 s.
    test.setTimeout(90_000);
    const external = trackExternalRequests(page);

    await page.goto('/diagnostic/');

    await expect(page.getByRole('heading', { name: fr.diagnostic.title })).toBeVisible();
    await expect(page.getByText(fr.diagnostic.verdicts.Good)).toBeVisible({ timeout: 60_000 });
    await expect(page.getByRole('button', { name: fr.diagnostic.retry })).toBeVisible();
    await expect(page.getByText(/^aller-retour \d+ ms · incertitude ±\d+ ms$/)).toBeVisible();

    await page.getByRole('button', { name: fr.diagnostic.flash.start }).click();
    const flash = page.getByRole('button', { name: fr.diagnostic.flash.stop });
    // White at each whole second of the server, black in between: checked at every frame, since
    // a white of 100 ms slips between the polls of an assertion.
    for (const color of ['rgb(255, 255, 255)', 'rgb(0, 0, 0)']) {
        await page.waitForFunction(
            (expected) =>
                getComputedStyle(document.querySelector('.flash')!).backgroundColor === expected,
            color,
            { polling: 'raf', timeout: 5_000 },
        );
    }
    await flash.click();
    await expect(flash).toBeHidden();
    expect(external).toEqual([]);

    const console = await openConsole(browser, baseURL);
    const section = console.locator('details', { hasText: fr.gm.network.title });
    // Open in the lobby only: another test may have started the game of the shared server.
    if (!(await section.evaluate((details) => (details as HTMLDetailsElement).open))) {
        await section.getByText(fr.gm.network.title).click();
    }
    await expect(section.getByText(/^http:\/\/192\.168\.1\.42:\d+\/diagnostic\/$/)).toBeVisible();
    await expect(section.getByRole('img', { name: fr.gm.network.qrLabel })).toBeVisible();
    const device =
        testInfo.project.name === 'ios-safari'
            ? fr.gm.network.devices.IPhone
            : fr.gm.network.devices.Android;
    await expect(
        section
            .getByRole('list', { name: fr.gm.network.diagnosticsLabel })
            .getByText(`${device} à`)
            .filter({ hasText: fr.gm.network.verdicts.Good })
            .first(),
    ).toBeVisible();
    await console.context().close();
});
