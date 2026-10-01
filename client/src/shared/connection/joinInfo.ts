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

/** How long the TV screen waits before asking again for an address it could not get. */
export const joinInfoRetryMs = 5000;

/**
 * Asks for the join address until the server knows one, then hands it over once. A missing
 * server or address is retried quietly, so the TV screen recovers without anyone reloading it.
 * Returns a function that stops the retries.
 */
export function watchJoinAddress(
    onAddress: (address: string | null) => void,
    fetcher: typeof fetch = fetch,
    retryMs: number = joinInfoRetryMs,
): () => void {
    let stopped = false;
    let timer: ReturnType<typeof setTimeout> | undefined;

    const attempt = async (): Promise<void> => {
        const address = (await fetchJoinInfo(fetcher))?.address ?? null;
        if (stopped) {
            return;
        }
        onAddress(address);
        if (address === null) {
            timer = setTimeout(() => void attempt(), retryMs);
        }
    };

    void attempt();
    return () => {
        stopped = true;
        clearTimeout(timer);
    };
}
