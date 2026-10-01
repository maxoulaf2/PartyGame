import { describe, expect, it } from 'vitest';
import { composeJoinUrl } from './joinUrl';

describe('composeJoinUrl', () => {
    it('keeps the port of the page', () => {
        expect(composeJoinUrl('192.168.1.42', { protocol: 'http:', port: '5000' })).toBe(
            'http://192.168.1.42:5000/',
        );
        expect(composeJoinUrl('10.0.0.2', { protocol: 'http:', port: '5173' })).toBe(
            'http://10.0.0.2:5173/',
        );
    });

    it('omits the default port', () => {
        expect(composeJoinUrl('192.168.1.42', { protocol: 'http:', port: '' })).toBe(
            'http://192.168.1.42/',
        );
    });
});
