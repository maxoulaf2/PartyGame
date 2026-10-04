import type { Browser, BrowserContextOptions, Page } from '@playwright/test';
import { fr } from '../src/shared/i18n/fr.ts';

/**
 * A nickname no other test can take: every test and browser project joins the same server.
 * Short enough to stay within the 16 visible characters.
 */
export function uniqueNickname(name: string): string {
    return `${name} ${Math.random().toString(36).slice(2, 7)}`;
}

/**
 * Opens the player page in a browser context of its own, emulating `device` if given, and joins
 * under `nickname`. `prepare` sets the page up before it opens, such as to relay its WebSockets.
 */
export async function joinOnNewPhone(
    browser: Browser,
    baseURL: string | undefined,
    nickname: string,
    device: BrowserContextOptions = {},
    prepare?: (phone: Page) => Promise<void>,
): Promise<Page> {
    const context = await browser.newContext({ ...device, baseURL });
    const phone = await context.newPage();
    await prepare?.(phone);
    await phone.goto('/');
    await phone.getByLabel(fr.player.join.label).fill(nickname);
    await phone.getByRole('button', { name: fr.player.join.submit }).click();
    // The form gives way once the server registered the player, whatever the phase shown then.
    await phone.getByLabel(fr.player.join.label).waitFor({ state: 'detached' });
    return phone;
}
