import { expect, test } from '@playwright/test';
import { fr } from '../src/shared/i18n/fr.ts';
import { trackExternalRequests } from './localRequests.ts';

test('player page shows its waiting text without external requests', async ({ page }) => {
    const external = trackExternalRequests(page);

    await page.goto('/');

    await expect(page.getByText(fr.player.waiting)).toBeVisible();
    expect(external).toEqual([]);
});

test('player page cannot be zoomed', async ({ page }) => {
    await page.goto('/');

    const viewport = await page.locator('meta[name="viewport"]').getAttribute('content');
    expect(viewport).toContain('user-scalable=no');
    expect(viewport).toContain('maximum-scale=1');
    const touchAction = await page.evaluate(
        () => getComputedStyle(document.documentElement).touchAction,
    );
    expect(touchAction).toBe('manipulation');
});
