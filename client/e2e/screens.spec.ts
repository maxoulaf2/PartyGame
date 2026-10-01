import { expect, test } from '@playwright/test';
import { fr } from '../src/shared/i18n/fr.ts';
import { trackExternalRequests } from './localRequests.ts';

const screens = [
    { path: '/display/', text: fr.display.waiting },
    { path: '/gm/', text: fr.gm.waiting },
];

for (const { path, text } of screens) {
    test(`${path} shows its waiting text without external requests`, async ({ page }) => {
        const external = trackExternalRequests(page);

        await page.goto(path);

        await expect(page.getByText(text)).toBeVisible();
        expect(external).toEqual([]);
    });
}
