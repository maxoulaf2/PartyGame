import { describe, expect, it, vi } from 'vitest';
import { fetchJoinInfo, parseJoinInfo } from './joinInfo';

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
