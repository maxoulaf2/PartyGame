import { afterEach, describe, expect, it, vi } from 'vitest';
import { fetchJoinInfo, parseJoinInfo, watchJoinAddress } from './joinInfo';

describe('parseJoinInfo', () => {
    it('accepts an address or a null address', () => {
        expect(parseJoinInfo({ address: '192.168.1.42' })).toEqual({ address: '192.168.1.42' });
        expect(parseJoinInfo({ address: null })).toEqual({ address: null });
    });

    it('rejects any other shape', () => {
        for (const body of [null, 'text', 42, {}, { address: 42 }, { address: undefined }]) {
            expect(parseJoinInfo(body), JSON.stringify(body)).toBeNull();
        }
    });

    it('keeps only the address', () => {
        expect(parseJoinInfo({ address: '10.0.0.2', extra: true })).toEqual({
            address: '10.0.0.2',
        });
    });
});

describe('fetchJoinInfo', () => {
    it('returns the info served by the server', async () => {
        const fetcher = vi.fn(async () => Response.json({ address: '192.168.1.42' }));

        await expect(fetchJoinInfo(fetcher)).resolves.toEqual({ address: '192.168.1.42' });
        expect(fetcher).toHaveBeenCalledWith('/api/join', { cache: 'no-store' });
    });

    it('returns null when the server fails or is unreachable', async () => {
        await expect(
            fetchJoinInfo(async () => new Response('', { status: 502 })),
        ).resolves.toBeNull();
        await expect(fetchJoinInfo(async () => new Response('<html>'))).resolves.toBeNull();
        await expect(
            fetchJoinInfo(async () => {
                throw new TypeError('Failed to fetch');
            }),
        ).resolves.toBeNull();
    });
});

describe('watchJoinAddress', () => {
    afterEach(() => {
        vi.useRealTimers();
    });

    it('retries until the server knows an address, then stops asking', async () => {
        vi.useFakeTimers();
        const answers = [
            () => new Response('', { status: 502 }),
            () => Response.json({ address: null }),
            () => Response.json({ address: '192.168.1.42' }),
        ];
        const fetcher = vi.fn(async () => {
            const answer = answers.shift();
            if (!answer) {
                throw new Error('Asked once too often');
            }
            return answer();
        });
        const onAddress = vi.fn();

        watchJoinAddress(onAddress, fetcher, 1000);
        await vi.advanceTimersByTimeAsync(0);
        expect(onAddress).toHaveBeenLastCalledWith(null);

        await vi.advanceTimersByTimeAsync(1000);
        expect(onAddress).toHaveBeenLastCalledWith(null);

        await vi.advanceTimersByTimeAsync(1000);
        expect(onAddress).toHaveBeenLastCalledWith('192.168.1.42');

        await vi.advanceTimersByTimeAsync(10_000);
        expect(fetcher).toHaveBeenCalledTimes(3);
        expect(onAddress).toHaveBeenCalledTimes(3);
    });

    it('stops retrying once stopped', async () => {
        vi.useFakeTimers();
        const fetcher = vi.fn(async () => Response.json({ address: null }));
        const onAddress = vi.fn();

        const stop = watchJoinAddress(onAddress, fetcher, 1000);
        await vi.advanceTimersByTimeAsync(0);
        stop();
        await vi.advanceTimersByTimeAsync(10_000);

        expect(fetcher).toHaveBeenCalledTimes(1);
    });
});
