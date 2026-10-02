import type { Browser, Page } from '@playwright/test';
import { fr } from '../src/shared/i18n/fr.ts';

/**
 * A nickname no other test can take: every test and browser project joins the same server.
 * Short enough to stay within the 16 visible characters.
 */
export function uniqueNickname(name: string): string {
    return `${name} ${Math.random().toString(36).slice(2, 7)}`;
}

/** Opens the player page in a browser context of its own and joins under `nickname`. */
export async function joinOnNewPhone(
    browser: Browser,
    baseURL: string | undefined,
    nickname: string,
): Promise<Page> {
    const context = await browser.newContext({ baseURL });
    const phone = await context.newPage();
    await phone.goto('/');
    await phone.getByLabel(fr.player.join.label).fill(nickname);
    await phone.getByRole('button', { name: fr.player.join.submit }).click();
    await phone.getByText(fr.player.registeredAs.replace('{nickname}', () => nickname)).waitFor();
    return phone;
}
