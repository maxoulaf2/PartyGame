import type { Page } from '@playwright/test';

/** Records every request that leaves localhost, to prove a page needs nothing from outside. */
export function trackExternalRequests(page: Page): string[] {
    const external: string[] = [];
    page.on('request', (request) => {
        const url = new URL(request.url());
        if (url.protocol !== 'data:' && url.hostname !== 'localhost') {
            external.push(request.url());
        }
    });
    return external;
}
