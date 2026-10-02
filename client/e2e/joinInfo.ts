import type { Page } from '@playwright/test';
import type { JoinInfo } from '../src/shared/contracts';

/** The address the server detects depends on the machine: the TV screen gets a fixed one from here. */
export async function serveJoinInfo(
    page: Page,
    info: JoinInfo = { address: '192.168.1.42' },
): Promise<void> {
    await page.route('**/api/join', (route) => route.fulfill({ json: info }));
}
