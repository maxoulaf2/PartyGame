import { expect, test } from '@playwright/test';
import { fr } from '../src/shared/i18n/fr.ts';
import { blockHub } from './fakeHub.ts';
import { trackExternalRequests } from './localRequests.ts';

// Its invitation rather than the player list, which the player tests change on the shared server.
test('/display/ invites players to join without external requests', async ({ page }) => {
    const external = trackExternalRequests(page);

    await page.goto('/display/');

    await expect(page.getByText(fr.display.scanToJoin)).toBeVisible();
    expect(external).toEqual([]);
});

test('/display/ shows a neutral message when the server does not answer', async ({ page }) => {
    await blockHub(page);

    await page.goto('/display/');

    await expect(page.getByText(fr.display.waiting)).toBeVisible();
    await expect(page.getByRole('img', { name: fr.display.qrCodeLabel })).toHaveCount(0);
});
