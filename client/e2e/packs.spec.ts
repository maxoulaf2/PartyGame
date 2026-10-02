import { expect, test, type Page } from '@playwright/test';
import { gameMasterCodeKey } from '../src/shared/connection/codeStorage.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { gameMasterCode, playedPack } from './gameServer.ts';

// Every browser project chooses the same pack on the shared server, so that they never contradict
// each other, and the launch, run last, plays it.

async function openConsole(page: Page): Promise<void> {
    await page.addInitScript(
        ([key, code]) => {
            localStorage.setItem(key, code);
        },
        [gameMasterCodeKey, gameMasterCode] as const,
    );
    await page.goto('/gm/');
    await expect(page.getByRole('heading', { name: fr.gm.consoleTitle })).toBeVisible();
}

test('the game master chooses a pack, which the TV screen announces', async ({
    page,
    browser,
    baseURL,
}) => {
    const display = await (await browser.newContext({ baseURL })).newPage();
    await display.goto('/display/');
    await openConsole(page);
    const packs = page.getByRole('list', { name: fr.gm.packs.listLabel });
    const played = packs.getByRole('listitem').filter({ hasText: playedPack.title });

    // Each pack shows its folder and its rounds, with the game mode that plays them.
    await expect(played).toContainText(fr.gm.packs.folder.replace('{folder}', playedPack.id));
    await expect(played).toContainText('Échauffement (Quiz QCM)');
    await expect(played).toContainText('Finale (Quiz QCM)');

    await page.getByRole('radio', { name: playedPack.title }).check();

    await expect(page.getByRole('radio', { name: playedPack.title })).toBeChecked();
    await expect(page.getByText(fr.gm.start.packRequired)).toHaveCount(0);
    await expect(
        display.getByText(fr.display.packTitle.replace('{title}', () => playedPack.title)),
    ).toBeVisible();

    await display.context().close();
});

test('the game master sees why an invalid pack cannot be chosen', async ({ page }) => {
    await openConsole(page);
    const broken = page
        .getByRole('list', { name: fr.gm.packs.listLabel })
        .getByRole('listitem')
        .filter({ hasText: 'Pack cassé' })
        .first();

    await expect(page.getByRole('radio', { name: 'Pack cassé' })).toBeDisabled();
    await expect(broken).toContainText('Invalide : 2 problèmes');

    await broken.getByText(fr.gm.packs.showProblems).click();

    await expect(broken.getByText('Média introuvable : images/tour-eiffel.jpg')).toBeVisible();
    await expect(
        broken.getByText('pack.json, $.rounds[0].questions[0].image', { exact: true }),
    ).toBeVisible();
    await expect(
        broken.getByText(fr.gm.packs.problems.QuizCorrectChoiceMissing, { exact: true }),
    ).toBeVisible();
});

test('the game master reloads the packs and keeps the catalog', async ({ page }) => {
    await openConsole(page);
    const reload = page.getByRole('button', { name: fr.gm.packs.reload });

    await reload.click();

    await expect(reload).toBeEnabled();
    await expect(page.getByText(fr.gm.packs.reloadFailed)).toHaveCount(0);
    await expect(page.getByRole('radio')).toHaveCount(3);
});
