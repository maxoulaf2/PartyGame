import { expect, test, type Locator, type Page } from '@playwright/test';
import type { ClientErrorReport, Role } from '../src/shared/contracts';
import { fr } from '../src/shared/i18n/fr.ts';

/** Ends each message of the SignalR JSON protocol. */
const recordSeparator = '\u001e';

/** Relays the WebSockets of the page to the server, and records the error reports it sends. */
async function recordErrorReports(page: Page): Promise<ClientErrorReport[]> {
    const reports: ClientErrorReport[] = [];
    await page.routeWebSocket(/\/hub\/game/, (toPage) => {
        const toServer = toPage.connectToServer();
        toPage.onMessage((message) => {
            for (const record of String(message).split(recordSeparator)) {
                if (record.includes('"ReportClientError"')) {
                    const invocation = JSON.parse(record) as { arguments: [ClientErrorReport] };
                    reports.push(invocation.arguments[0]);
                }
            }
            toServer.send(message);
        });
        toServer.onMessage((message) => toPage.send(message));
    });
    return reports;
}

/** Throws from an event handler, where no boundary sees it, and rejects a promise nobody handles. */
async function failOutsideAnyHandler(page: Page): Promise<void> {
    await page.evaluate(() => {
        setTimeout(() => {
            throw new Error('e2e uncaught');
        }, 0);
        void Promise.reject(new Error('e2e rejected'));
    });
}

/** Each page, with what it shows once connected, whatever other tests do on the shared server. */
const pages: { path: string; role: Role; landmark: (page: Page) => Locator }[] = [
    { path: '/', role: 'Player', landmark: (page) => page.getByLabel(fr.player.join.label) },
    {
        path: '/display/',
        role: 'Display',
        landmark: (page) =>
            page.getByText(fr.display.scanToJoin).or(page.getByText(fr.display.joinUnavailable)),
    },
    { path: '/gm/', role: 'GameMaster', landmark: (page) => page.getByLabel(fr.gm.code.label) },
];

for (const { path, role, landmark } of pages) {
    test(`the ${role} page reports its errors to the server, without showing anything`, async ({
        page,
    }) => {
        const reports = await recordErrorReports(page);
        await page.goto(path);
        await expect(landmark(page)).toBeVisible();

        await failOutsideAnyHandler(page);

        await expect
            .poll(() => reports.map((report) => report.kind).sort())
            .toEqual(['Error', 'UnhandledRejection']);
        for (const report of reports) {
            expect(report).toMatchObject({ role, page: path });
        }
        expect(reports.map((report) => report.message).sort()).toEqual([
            'Error: e2e rejected',
            'Error: e2e uncaught',
        ]);
        await expect(landmark(page)).toBeVisible();
        await expect(page.getByText(/e2e/)).toHaveCount(0);
    });
}
