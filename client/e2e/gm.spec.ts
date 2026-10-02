import { expect, test, type Page } from '@playwright/test';
import { gameMasterCodeKey } from '../src/shared/connection/codeStorage.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { gameMasterCode } from './gameServer.ts';
import { trackExternalRequests } from './localRequests.ts';

/** Records the target of every hub message the page receives. */
function trackHubMessages(page: Page): string[] {
    const targets: string[] = [];
    page.on('websocket', (socket) => {
        socket.on('framereceived', ({ payload }) => {
            // The JSON protocol separates messages with the 0x1e record separator.
            for (const message of String(payload).split('\u001e')) {
                const target = /"target":"([^"]+)"/.exec(message)?.[1];
                if (target) {
                    targets.push(target);
                }
            }
        });
    });
    return targets;
}

test('/gm/ asks for the code, refuses a wrong one, then remembers the right one', async ({
    page,
}) => {
    const external = trackExternalRequests(page);
    const received = trackHubMessages(page);
    await page.goto('/gm/');

    const field = page.getByLabel(fr.gm.code.label);
    const submit = page.getByRole('button', { name: fr.gm.code.submit });
    await expect(field).toHaveAttribute('inputmode', 'numeric');
    await field.fill('12345');
    await expect(submit).toBeDisabled();

    await field.fill('000000');
    await expect(submit).toBeEnabled();
    await submit.click();

    await expect(page.getByText(fr.gm.code.invalid)).toBeVisible();
    await expect(field).toHaveValue('');
    await expect(field).toBeFocused();
    expect(received).not.toContain('ReceiveGameMasterSnapshot');

    await field.fill(` ${gameMasterCode} `);
    await submit.click();

    await expect(page.getByRole('heading', { name: fr.gm.consoleTitle })).toBeVisible();
    expect(received).toContain('ReceiveGameMasterSnapshot');

    await page.reload();

    await expect(page.getByRole('heading', { name: fr.gm.consoleTitle })).toBeVisible();
    await expect(page.getByLabel(fr.gm.code.label)).toHaveCount(0);
    expect(external).toEqual([]);
});

test('/gm/ asks for the code again when the remembered one is no longer valid', async ({
    page,
}) => {
    await page.addInitScript((key) => {
        localStorage.setItem(key, '000000');
    }, gameMasterCodeKey);

    await page.goto('/gm/');

    await expect(page.getByText(fr.gm.code.expired)).toBeVisible();
    await expect(page.getByLabel(fr.gm.code.label)).toBeVisible();
    expect(await page.evaluate((key) => localStorage.getItem(key), gameMasterCodeKey)).toBeNull();
});
