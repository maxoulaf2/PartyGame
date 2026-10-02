import { expect, test } from '@playwright/test';
import { fr } from '../src/shared/i18n/fr.ts';

// Addresses typed by hand by the operator: a missing slash or another case still opens the right page.
const addresses = [
    { typed: '/display', page: '/display/', text: fr.display.scanToJoin },
    { typed: '/Display/', page: '/display/', text: fr.display.scanToJoin },
    { typed: '/gm', page: '/gm/', text: fr.gm.code.title },
    { typed: '/GM', page: '/gm/', text: fr.gm.code.title },
];

for (const { typed, page: canonical, text } of addresses) {
    test(`${typed} opens ${canonical}`, async ({ page }) => {
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
