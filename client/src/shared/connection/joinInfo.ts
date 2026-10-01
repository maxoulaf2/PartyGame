import type { JoinInfo } from '../contracts';

const joinInfoUrl = '/api/join';

/** Narrows an untrusted response body to a JoinInfo, or null if it does not have that shape. */
export function parseJoinInfo(body: unknown): JoinInfo | null {
    if (typeof body !== 'object' || body === null || !('address' in body)) {
        return null;
    }
    const { address } = body;
    return typeof address === 'string' || address === null ? { address } : null;
}

/**
 * Asks the server for the address phones join at. Resolves to null instead of throwing when the
 * server cannot answer: the TV screen then keeps a neutral message, never an error.
 */
export async function fetchJoinInfo(fetcher: typeof fetch = fetch): Promise<JoinInfo | null> {
    try {
        const response = await fetcher(joinInfoUrl, { cache: 'no-store' });
        return response.ok ? parseJoinInfo(await response.json()) : null;
    } catch {
        return null;
    }
}
