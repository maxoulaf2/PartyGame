import { expect, test } from '@playwright/test';
import { fr } from '../src/shared/i18n/fr.ts';
import { serveJoinInfo } from './joinInfo.ts';
import { trackExternalRequests } from './localRequests.ts';

test('/display/ shows its waiting text without external requests', async ({ page }) => {
    const external = trackExternalRequests(page);
    await serveJoinInfo(page);

    await page.goto('/display/');

    await expect(page.getByText(fr.display.waiting)).toBeVisible();
    expect(external).toEqual([]);
});

test('/display/ shows a neutral message when the server knows no address', async ({ page }) => {
    await serveJoinInfo(page, { address: null });

    await page.goto('/display/');

    await expect(page.getByText(fr.display.joinUnavailable)).toBeVisible();
});

test('/display/ shows a neutral message when the server does not answer', async ({ page }) => {
    await page.route('**/api/join', (route) => route.fulfill({ status: 502 }));

    await page.goto('/display/');

    await expect(page.getByText(fr.display.joinUnavailable)).toBeVisible();
});
