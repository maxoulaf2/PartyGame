/**
 * Where a page keeps a value of its user between visits, such as the game master code or the
 * token of a player, so that it is not asked again.
 */
export interface CodeStorage {
    load(): string | null;
    save(code: string): void;
    clear(): void;
}

/** The key the game master code is kept under: specific to the GM console. */
export const gameMasterCodeKey = 'partygame.gm.code';

/** The key the token of a player is kept under: the only proof of who they are after a reload. */
export const playerTokenKey = 'partygame.player.token';

/** The key the last nickname a player joined with is kept under, to fill the form again. */
export const playerNicknameKey = 'partygame.player.nickname';

/**
 * The key, in the `sessionStorage` of the tab, of the build the page last reloaded to get: a
 * reload that brings back the same outdated page must not start over.
 */
export const reloadTargetKey = 'partygame.build.reloadTarget';

/**
 * Keeps a value in the `localStorage`, which plain HTTP allows. A browser that refuses storage
 * (private browsing, disabled cookies) only means the value is asked again at the next visit.
 */
export function localCodeStorage(key: string, storage?: Storage): CodeStorage {
    return webCodeStorage(key, () => storage ?? globalThis.localStorage);
}

/**
 * Keeps a value in the `sessionStorage`: for the tab only, across its reloads. A browser that
 * refuses storage forgets the value at once.
 */
export function sessionCodeStorage(key: string, storage?: Storage): CodeStorage {
    return webCodeStorage(key, () => storage ?? globalThis.sessionStorage);
}

function webCodeStorage(key: string, storage: () => Storage): CodeStorage {
    const target = (): Storage | null => {
        try {
            return storage();
        } catch {
            // Reading `localStorage` or `sessionStorage` itself throws when the browser blocks storage.
            return null;
        }
    };
    return {
        load: () => {
            try {
                return target()?.getItem(key) ?? null;
            } catch {
                return null;
            }
        },
        save: (code) => {
            try {
                target()?.setItem(key, code);
            } catch {
                // Quota or policy: the code simply will not be remembered.
            }
        },
        clear: () => {
            try {
                target()?.removeItem(key);
            } catch {
                // Nothing remembered, nothing to forget.
            }
        },
    };
}
