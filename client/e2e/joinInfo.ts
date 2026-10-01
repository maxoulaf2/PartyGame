import type { Page } from '@playwright/test';
import type { JoinInfo } from '../src/shared/contracts';

/** The preview server has no .NET server behind its proxy: the TV screen gets its join info from here. */
export async function serveJoinInfo(
    page: Page,
    info: JoinInfo = { address: '192.168.1.42' },
): Promise<void> {
    await page.route('**/api/join', (route) => route.fulfill({ json: info }));
}
