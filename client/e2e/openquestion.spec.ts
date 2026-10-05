import { fileURLToPath } from 'node:url';
import type { Page } from '@playwright/test';
import { fill } from '../src/shared/i18n/fill.ts';
import { fr } from '../src/shared/i18n/fr.ts';
import { formatNumber } from '../src/shared/i18n/numberText.ts';
import { expect, test } from './fixtures/table.ts';

// A round of open questions on a table of its own: each player types their answer on their phone,
// Zoé on an iPhone under WebKit, Max and Léa on Pixels under Chromium.

/** A single pack, chosen at once: a question in text, then a numeric one of 5 seconds. */
const openQuestionPacks = fileURLToPath(new URL('./openquestion-packs', import.meta.url));

test.use({ serverPacks: openQuestionPacks });

const texts = fr.modes.openquestion;

async function startGame(gm: Page): Promise<void> {
    await gm.getByRole('button', { name: fr.gm.start.action, exact: true }).click();
    await gm
        .getByRole('dialog', { name: fr.gm.start.confirmTitle })
        .getByRole('button', { name: fr.gm.start.confirm, exact: true })
        .click();
}

function field(phone: Page) {
    return phone.getByLabel(texts.player.answerLabel);
}

function answered(count: number) {
    return fill(texts.answered, { answered: count, participants: 3 });
}

async function skipQuestion(gm: Page): Promise<void> {
    await gm.getByRole('button', { name: texts.gm.skipQuestion }).click();
    await gm
        .getByRole('dialog')
        .getByRole('button', { name: texts.gm.skipConfirm.confirm })
        .click();
}

test('the players type their answer on their phones until every one answered', async ({
    table,
}) => {
    const { display, gm } = table;
    const [zoe, max, lea] = table.players;
    await startGame(gm);

    // The question is presented: its number on the TV screen, its text and its answer on the
    // console, a field still closed on the phones.
    await expect(
        display.getByText(fill(texts.display.upcoming, { number: 1 }), { exact: true }),
    ).toBeVisible();
    await expect(display.getByText('Réponses libres')).toBeVisible();
    await expect(display.getByText('Joconde')).toHaveCount(0);
    await expect(gm.getByText('Qui a peint La Joconde ?')).toBeVisible();
    await expect(
        gm.getByText(fill(texts.gm.expectedAnswer, { answer: 'Léonard de Vinci' })),
    ).toBeVisible();
    for (const player of table.players) {
        await expect(field(player.page)).toBeDisabled();
    }

    // The game master shows it: on the TV screen, and the fields open with the countdown.
    await gm.getByRole('button', { name: texts.gm.showQuestion }).click();
    await expect(display.getByRole('heading', { name: 'Qui a peint La Joconde ?' })).toBeVisible();
    await expect(display.getByText(answered(0))).toBeVisible();
    for (const player of table.players) {
        const input = field(player.page);
        await expect(input).toBeEnabled();
        await expect(input).toHaveAttribute('inputmode', 'text');
        await expect(input).toHaveAttribute('autocapitalize', 'off');
        await expect(input).toHaveAttribute('autocorrect', 'off');
        await expect(input).toHaveAttribute('maxlength', '30');
        await expect(player.page.getByRole('timer')).toBeVisible();
    }

    // Zoé types her answer: it survives a reload before she sends it, with the Enter key.
    await field(zoe.page).fill('Léonard de Vinci');
    await zoe.page.reload();
    await expect(field(zoe.page)).toHaveValue('Léonard de Vinci');
    await expect(field(zoe.page)).toBeEnabled();
    await field(zoe.page).press('Enter');
    await expect(zoe.page.getByText(texts.player.recorded)).toBeVisible();
    await expect(field(zoe.page)).toBeDisabled();

    // The TV screen counts it, the console shows it, the other phones know nothing of it.
    await expect(display.getByText(answered(1))).toBeVisible();
    await expect(
        gm.getByRole('list', { name: texts.gm.answersLabel }).getByText('Léonard de Vinci'),
    ).toBeVisible();
    await expect(display.getByText('Léonard de Vinci')).toHaveCount(0);
    await expect(max.page.getByText('Léonard de Vinci')).toHaveCount(0);
    await expect(field(max.page)).toHaveValue('');

    // Max and Léa answer with the button: once every one answered, the answers lock.
    await field(max.page).fill('Picasso');
    await max.page.getByRole('button', { name: texts.player.send }).tap();
    await field(lea.page).fill('De Vinci');
    await lea.page.getByRole('button', { name: texts.player.send }).tap();
    await expect(display.getByText(texts.allAnswered)).toBeVisible();
    await expect(max.page.getByText(texts.player.recorded)).toBeVisible();
    await expect(lea.page.getByText(texts.player.recorded)).toBeVisible();
    await expect(display.getByText('Picasso')).toHaveCount(0);
});

test('the countdown locks the answers of a numeric question', async ({ table }) => {
    const { display, gm } = table;
    const [zoe, max, lea] = table.players;
    await startGame(gm);
    await skipQuestion(gm);

    // The second question asks for a number: the phones open a numeric keyboard.
    await expect(
        display.getByText(fill(texts.display.upcoming, { number: 2 }), { exact: true }),
    ).toBeVisible();
    await gm.getByRole('button', { name: texts.gm.showQuestion }).click();
    await expect(field(zoe.page)).toHaveAttribute('inputmode', 'numeric');
    await field(zoe.page).fill('1969');
    await field(zoe.page).press('Enter');
    await expect(display.getByText(answered(1))).toBeVisible();

    // Max types without sending: the end of the countdown locks the answers.
    await field(max.page).fill('1970');
    await expect(display.getByText(texts.timeUp)).toBeVisible({ timeout: 10_000 });
    await expect(max.page.getByText(texts.timeUp)).toBeVisible();
    await expect(lea.page.getByText(texts.timeUp)).toBeVisible();
    await expect(field(max.page)).toBeDisabled();
    await expect(zoe.page.getByText(texts.player.recorded)).toBeVisible();
    await expect(
        gm.getByText(fill(texts.gm.withoutAnswer, { nicknames: 'Max, Léa' })),
    ).toBeVisible();
});

test('the game master judges the answers in one go, reveals them, and plays the round to its end', async ({
    table,
}) => {
    const { display, gm } = table;
    const [zoe, max, lea] = table.players;
    await startGame(gm);
    await gm.getByRole('button', { name: texts.gm.showQuestion }).click();

    for (const [player, answer] of [
        [zoe, 'Léonard de Vinci'],
        [max, 'Picasso'],
        [lea, 'Leonard de Vinchi'],
    ] as const) {
        await field(player.page).fill(answer);
        await field(player.page).press('Enter');
    }

    // The console groups the answers, pre-classified: the accepted one checked, the others not.
    const groups = gm.getByRole('list', { name: texts.gm.groupsLabel }).getByRole('listitem');
    await expect(groups).toHaveCount(3);
    await expect(groups.nth(0)).toContainText(texts.gm.categories.Accepted);
    await expect(groups.nth(1)).toContainText(texts.gm.categories.ToCheck);
    await expect(groups.nth(2)).toContainText(texts.gm.categories.Rejected);
    await expect(groups.nth(0).getByRole('checkbox')).toBeChecked();
    await expect(groups.nth(1).getByRole('checkbox')).not.toBeChecked();
    await expect(groups.nth(2).getByRole('checkbox')).not.toBeChecked();

    // The TV screen only says the game master checks them.
    await expect(display.getByText(texts.display.checking)).toBeVisible();
    await expect(display.getByText('Vinchi')).toHaveCount(0);

    // The game master accepts the typo too, then validates the whole batch.
    await groups.nth(1).getByRole('checkbox').check();
    await gm.getByRole('button', { name: texts.gm.validate }).click();
    await expect(groups.getByText(texts.gm.right)).toHaveCount(2);
    await expect(groups.nth(2)).toContainText(texts.gm.wrong);
    await expect(gm.getByRole('button', { name: texts.gm.validate })).toHaveCount(0);

    // No verdict anywhere else before the reveal.
    await expect(display.getByText(texts.display.checking)).toBeVisible();
    for (const player of table.players) {
        await expect(player.page.getByText(texts.player.recorded)).toBeVisible();
        await expect(player.page.getByText(texts.gm.right)).toHaveCount(0);
    }

    // The reveal: the expected answer, then the answers with their authors, the right ones first.
    await gm.getByRole('button', { name: texts.gm.revealAnswer }).click();
    await expect(display.getByText('Léonard de Vinci', { exact: true })).toHaveCount(2);
    const revealed = display
        .getByRole('list', { name: texts.display.answersLabel })
        .getByRole('listitem');
    await expect(revealed).toHaveCount(3);
    await expect(revealed.nth(0)).toContainText('Zoé');
    await expect(revealed.nth(1)).toContainText(/Leonard de Vinchi\s*Léa/);
    await expect(revealed.nth(2)).toContainText(/Picasso\s*Max/);
    await expect(revealed.nth(1).getByRole('img', { name: texts.display.right })).toBeVisible();
    await expect(revealed.nth(2).getByRole('img', { name: texts.display.wrong })).toBeVisible();

    // Each phone tells its own verdict and points, never the answers of the others.
    const points = fill(texts.pointsEarned, { points: formatNumber(1000) });
    await expect(zoe.page.getByText(texts.player.verdicts.Correct)).toBeVisible();
    await expect(max.page.getByText(texts.player.verdicts.Wrong)).toBeVisible();
    await expect(lea.page.getByText(texts.player.verdicts.Correct)).toBeVisible();
    await expect(
        max.page.getByText(fill(texts.player.expectedAnswer, { answer: 'Léonard de Vinci' })),
    ).toBeVisible();
    await expect(max.page.getByText('Vinchi')).toHaveCount(0);
    await expect(gm.getByRole('button', { name: texts.gm.skipQuestion })).toHaveCount(0);
    await expect(groups.nth(0)).toContainText(`Zoé (${points})`);

    // Then the last question: nobody answers in time, and the round ends.
    await gm.getByRole('button', { name: texts.gm.nextQuestion }).click();
    await gm.getByRole('button', { name: texts.gm.showQuestion }).click();
    await expect(display.getByText(texts.timeUp)).toBeVisible({ timeout: 10_000 });
    await gm.getByRole('button', { name: texts.gm.revealAnswer }).click();
    await expect(display.getByText(texts.display.unanswered)).toBeVisible();
    await expect(zoe.page.getByText(texts.player.verdicts.NoAnswer)).toBeVisible();
    await gm.getByRole('button', { name: texts.gm.endRound }).click();
    await expect(display.getByText(fr.game.finished)).toBeVisible();
});
