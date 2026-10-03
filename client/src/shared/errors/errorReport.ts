/**
 * The longest each field of a report may be, as the server cuts them too (`ClientErrorFields`):
 * the beginning of a message or of a stack trace is enough to find a bug, and a report stays small.
 */
export const reportFieldLimits = {
    page: 200,
    message: 500,
    stack: 2000,
    roundViewType: 64,
    buildId: 64,
} as const;

/** What a report tells about an error: its message, and its stack trace when the browser has one. */
export interface ErrorDescription {
    readonly message: string;
    readonly stack: string | null;
}

/**
 * `value`, cut to `maxLength` characters at most, never in the middle of a surrogate pair (an
 * emoji, for instance), which would leave half a character.
 */
export function truncate(value: string, maxLength: number): string {
    if (value.length <= maxLength) {
        return value;
    }
    const last = value.charCodeAt(maxLength - 1);
    const cut = last >= 0xd800 && last <= 0xdbff ? maxLength - 1 : maxLength;
    return value.slice(0, cut);
}

/**
 * Describes whatever was thrown or rejected: an `Error` most of the time, but a promise may be
 * rejected with anything, a string or a plain object.
 */
export function describeError(error: unknown): ErrorDescription {
    if (error instanceof Error) {
        return {
            message: `${error.name}: ${error.message}`,
            stack: typeof error.stack === 'string' && error.stack !== '' ? error.stack : null,
        };
    }
    if (typeof error === 'string') {
        return { message: error, stack: null };
    }
    return { message: textOf(error), stack: null };
}

function textOf(value: unknown): string {
    try {
        return JSON.stringify(value) ?? String(value);
    } catch {
        // A cycle, or an object without prototype: its type is all there is to tell.
        return `Unreadable ${typeof value}`;
    }
}

/** Schemes of the scripts browser extensions inject in the pages, whose errors are none of ours. */
const extensionSchemes = [
    'chrome-extension://',
    'moz-extension://',
    'safari-extension://',
    'safari-web-extension://',
];

/**
 * Whether an error is noise rather than a bug of the pages:
 * - the « ResizeObserver loop » warnings browsers raise as errors, harmless;
 * - « Script error. » without a stack, all a browser tells of an error of another origin;
 * - errors of scripts injected by browser extensions, from `source`, the script the browser blames.
 */
export function isIgnoredError(description: ErrorDescription, source: string | null): boolean {
    const { message, stack } = description;
    if (message.includes('ResizeObserver loop')) {
        return true;
    }
    if (stack === null && /^(\w+: )?Script error\.?$/.test(message)) {
        return true;
    }
    return source !== null && extensionSchemes.some((scheme) => source.startsWith(scheme));
}
