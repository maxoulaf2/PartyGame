import type { Page } from '@playwright/test';
import { gameMasterCodeKey } from '../src/shared/connection/codeStorage.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { expect, test } from './fixtures/table.ts';
import { advertisedAddress, gameMasterCode } from './gameServer.ts';

// Changing the advertised address changes the QR code of every TV screen: on a server of its own.

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

test('the address chosen by the game master reaches the TV screen without a reload', async ({
    page,
    browser,
    baseURL,
}) => {
    // The dedicated server serves the pages, on its own port.
    const joinUrl = (address: string) => `http://${address}:${new URL(baseURL ?? '').port}/`;
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
