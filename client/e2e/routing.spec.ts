import { expect, test } from '@playwright/test';
import { fr } from '../src/shared/i18n/fr.ts';
import { serveJoinInfo } from './joinInfo.ts';

// Addresses typed by hand by the operator: a missing slash or another case still opens the right page.
const addresses = [
    { typed: '/display', page: '/display/', text: fr.display.waiting },
    { typed: '/Display/', page: '/display/', text: fr.display.waiting },
    { typed: '/gm', page: '/gm/', text: fr.gm.waiting },
    { typed: '/GM', page: '/gm/', text: fr.gm.waiting },
];

for (const { typed, page: canonical, text } of addresses) {
    test(`${typed} opens ${canonical}`, async ({ page }) => {
        await serveJoinInfo(page);
        await page.goto(typed);

        await expect(page).toHaveURL(canonical);
        await expect(page.getByText(text)).toBeVisible();
    });
}

test('page redirects are temporary', async ({ request }) => {
    const response = await request.get('/display', { maxRedirects: 0 });

    expect(response.status()).toBe(302);
    expect(response.headers()['location']).toBe('/display/');
});
