import { expect, test } from '@playwright/test';
import { fr } from '../src/shared/i18n/fr.ts';
import { serveJoinInfo } from './joinInfo.ts';
import { trackExternalRequests } from './localRequests.ts';

// The preview server listens on the port set in playwright.config.ts: the QR code must keep it.
const expectedUrl = 'http://192.168.1.42:4173/';

test.use({ viewport: { width: 1920, height: 1080 } });

test('/display/ shows a QR code and the join url without external requests', async ({ page }) => {
    const external = trackExternalRequests(page);
    await serveJoinInfo(page);

    await page.goto('/display/');

    const qrCode = page.getByRole('img', { name: fr.display.qrCodeLabel });
    await expect(qrCode).toBeVisible();
    await expect(qrCode).toHaveAttribute('data-qr-text', expectedUrl);
    await expect(page.getByText(expectedUrl)).toBeVisible();
    await expect(page.getByText(fr.display.scanToJoin)).toBeVisible();
    expect(external).toEqual([]);
});

test('/display/ keeps the QR code and the url away from the cropped edges', async ({ page }) => {
    await serveJoinInfo(page);
    await page.goto('/display/');

    const viewport = page.viewportSize();
    if (!viewport) {
        throw new Error('The test needs a fixed viewport');
    }
    const marginX = viewport.width * 0.05;
    const marginY = viewport.height * 0.05;
    for (const element of [
        page.getByRole('img', { name: fr.display.qrCodeLabel }),
        page.getByText(expectedUrl),
    ]) {
        const box = await element.boundingBox();
        expect(box).not.toBeNull();
        if (box) {
            expect(box.x).toBeGreaterThanOrEqual(marginX);
            expect(box.y).toBeGreaterThanOrEqual(marginY);
            expect(box.x + box.width).toBeLessThanOrEqual(viewport.width - marginX);
            expect(box.y + box.height).toBeLessThanOrEqual(viewport.height - marginY);
        }
    }
});

test('/display/ shows the QR code once the server finally knows an address', async ({ page }) => {
    await page.clock.install();
    let info: { address: string | null } = { address: null };
    await page.route('**/api/join', (route) => route.fulfill({ json: info }));

    await page.goto('/display/');
    await expect(page.getByText(fr.display.joinUnavailable)).toBeVisible();

    info = { address: '192.168.1.42' };
    await page.clock.runFor(10_000);

    await expect(page.getByRole('img', { name: fr.display.qrCodeLabel })).toBeVisible();
    await expect(page.getByText(expectedUrl)).toBeVisible();
});
