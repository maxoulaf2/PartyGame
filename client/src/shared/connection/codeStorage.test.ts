import { describe, expect, it } from 'vitest';
import { localCodeStorage, sessionCodeStorage } from './codeStorage';

function memoryStorage(): Storage {
    const items = new Map<string, string>();
    return {
        get length() {
            return items.size;
        },
        clear: () => items.clear(),
        getItem: (key) => items.get(key) ?? null,
        key: (index) => [...items.keys()][index] ?? null,
        removeItem: (key) => {
            items.delete(key);
        },
        setItem: (key, value) => {
            items.set(key, value);
        },
    };
}

function blockedStorage(): Storage {
    const blocked = () => {
        throw new Error('SecurityError');
    };
    return {
        length: 0,
        clear: blocked,
        getItem: blocked,
        key: blocked,
        removeItem: blocked,
        setItem: blocked,
    };
}

describe('localCodeStorage', () => {
    it('keeps a code under its key until cleared', () => {
        const backing = memoryStorage();
        const storage = localCodeStorage('test.code', backing);

        expect(storage.load()).toBeNull();
        storage.save('123456');
        expect(backing.getItem('test.code')).toBe('123456');
        expect(storage.load()).toBe('123456');
        storage.clear();
        expect(storage.load()).toBeNull();
    });

    it('behaves as an empty storage when the browser blocks storage', () => {
        const storage = localCodeStorage('test.code', blockedStorage());

        expect(() => storage.save('123456')).not.toThrow();
        expect(storage.load()).toBeNull();
        expect(() => storage.clear()).not.toThrow();
    });
});

describe('sessionCodeStorage', () => {
    it('keeps a value under its key until cleared', () => {
        const backing = memoryStorage();
        const storage = sessionCodeStorage('test.build', backing);

        storage.save('b2');
        expect(backing.getItem('test.build')).toBe('b2');
        expect(storage.load()).toBe('b2');
        storage.clear();
        expect(storage.load()).toBeNull();
    });

    it('keeps nothing when the browser blocks storage', () => {
        const storage = sessionCodeStorage('test.build', blockedStorage());

        storage.save('b2');
        expect(storage.load()).toBeNull();
    });
});
