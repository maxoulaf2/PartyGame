import { expect, test, type Page } from '@playwright/test';
import { gameMasterCodeKey } from '../src/shared/connection/codeStorage.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { advertisedAddress, gameMasterCode } from './gameServer.ts';

// Changing the advertised address changes the QR code of every TV screen of the shared server:
// playwright.config.ts runs this file once all the other tests are done, and it puts the
// configured address back before it ends.

async function openConsole(page: Page): Promise<void> {
    await page.addInitScript(
        ([key, code]) => {
            localStorage.setItem(key, code);
        },
        [gameMasterCodeKey, gameMasterCode] as const,
    );
    await page.goto('/gm/');
    await expect(page.getByRole('heading', { name: fr.gm.consoleTitle })).toBeVisible();
}

function joinUrl(address: string): string {
    // The preview server listens on the port set in playwright.config.ts.
    return `http://${address}:4173/`;
}

test('the address chosen by the game master reaches the TV screen without a reload', async ({
    page,
    browser,
    baseURL,
}) => {
    const display = await (await browser.newContext({ baseURL })).newPage();
    await display.goto('/display/');
    await expect(display.getByText(joinUrl(advertisedAddress))).toBeVisible();
    await openConsole(page);

    // The configured address comes first; the others are those of the machine running the tests.
    const select = page.getByLabel(fr.gm.address.label);
    const addresses = (await select.count())
        ? await select
              .getByRole('option')
              .evaluateAll((options) => options.map((o) => (o as HTMLOptionElement).value))
        : [];
    const other = addresses.find((address) => address !== advertisedAddress);
    test.skip(other === undefined, 'this machine has no private IPv4 address to choose');
    if (other === undefined) {
        return;
    }
    await expect(select).toHaveValue(advertisedAddress);

    await select.selectOption(other);

    await expect(display.getByText(joinUrl(other))).toBeVisible({ timeout: 1_000 });
    await expect(select).toHaveValue(other);

    await select.selectOption(advertisedAddress);

    await expect(display.getByText(joinUrl(advertisedAddress))).toBeVisible({ timeout: 1_000 });
    await display.context().close();
});
