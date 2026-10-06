import type { PlayerIntentEnvelope, PlayerRoundIntent } from '../contracts';
import type { CodeStorage } from './codeStorage';

/**
 * The key the intents of a player not acknowledged yet are kept under, with the token they were
 * numbered for and the last number given.
 */
export const playerIntentsKey = 'partygame.player.intents';

/**
 * Hands an intent to the server. Resolves once the server acknowledged it, accepted or not;
 * rejects when the connection was lost first, the server having received it or not.
 */
export type IntentSender = (envelope: PlayerIntentEnvelope) => Promise<unknown>;

/** What the queue keeps between visits. */
interface KeptIntents {
    readonly token: string;
    readonly lastSeq: number;
    readonly pending: readonly PlayerIntentEnvelope[];
}

/**
 * The intents of a player on their way to the server. Each one is numbered (`clientSeq`) and kept,
 * in the `localStorage` as well, until the server acknowledges it: when the connection drops before
 * the acknowledgment, the phone cannot tell whether the server received it, and sends it again,
 * with the same number, once the player is identified again. The server ignores a number it
 * already handled, so an intent never counts twice.
 *
 * One intent at a time: the next one leaves once the previous one is acknowledged, so that the
 * server gets them in order.
 */
export class IntentQueue {
    #pending = $state.raw<readonly PlayerIntentEnvelope[]>([]);
    #token: string | null = null;
    #lastSeq = 0;
    #sender: IntentSender | null = null;
    #sending = false;

    readonly #storage: CodeStorage;

    constructor(storage: CodeStorage) {
        this.#storage = storage;
    }

    /** The intents not acknowledged yet, oldest first: the page shows them as pending. */
    get pending(): readonly PlayerRoundIntent[] {
        return this.#pending.map((envelope) => envelope.intent);
    }

    /**
     * Takes up the intents kept for `token`, such as those of a page reloaded before their
     * acknowledgment. Kept for another token, they are forgotten, and numbers start over.
     */
    restore(token: string): void {
        const kept = readKept(this.#storage.load());
        if (kept === null || kept.token !== token) {
            this.reset(token);
            return;
        }
        this.#token = token;
        this.#lastSeq = kept.lastSeq;
        this.#pending = kept.pending;
    }

    /**
     * Starts afresh for a token the server just issued, its numbers from 1, or for the token of a
     * player recovered by their code, its numbers after `lastSeq`, the last the server handled.
     */
    reset(token: string, lastSeq = 0): void {
        this.#token = token;
        this.#lastSeq = lastSeq;
        this.#pending = [];
        this.#save();
    }

    /** Forgets everything, the server no longer knowing the token: nothing is left to send. */
    clear(): void {
        this.#token = null;
        this.#lastSeq = 0;
        this.#pending = [];
        this.#storage.clear();
    }

    /**
     * Numbers the intent and queues it, to be sent as soon as the player is identified. Ignored
     * without a token: the server would not know whom it comes from.
     */
    enqueue(intent: PlayerRoundIntent): void {
        if (this.#token === null) {
            return;
        }
        this.#lastSeq++;
        this.#pending = [...this.#pending, { clientSeq: this.#lastSeq, intent }];
        this.#save();
        void this.#pump();
    }

    /**
     * The connection identified the player: the pending intents leave through `sender`, in order,
     * until the connection is closed.
     */
    open(sender: IntentSender): void {
        this.#sender = sender;
        void this.#pump();
    }

    /** The connection is lost: nothing leaves until it is opened again. */
    close(): void {
        this.#sender = null;
    }

    async #pump(): Promise<void> {
        if (this.#sending) {
            return;
        }
        this.#sending = true;
        try {
            for (
                let envelope = this.#pending[0];
                this.#sender !== null && envelope !== undefined;
                envelope = this.#pending[0]
            ) {
                const sender = this.#sender;
                try {
                    await sender(envelope);
                } catch {
                    if (this.#sender === sender) {
                        // Lost with the connection: sent again once the player is identified anew.
                        this.#sender = null;
                    }
                    // Otherwise a new connection took over meanwhile: sent again through it.
                    continue;
                }
                // Unless reset meanwhile: the queue then holds the intents of another token.
                if (this.#pending[0] === envelope) {
                    this.#pending = this.#pending.slice(1);
                    this.#save();
                }
            }
        } finally {
            this.#sending = false;
        }
    }

    #save(): void {
        if (this.#token === null) {
            return;
        }
        const kept: KeptIntents = {
            token: this.#token,
            lastSeq: this.#lastSeq,
            pending: this.#pending,
        };
        this.#storage.save(JSON.stringify(kept));
    }
}

/** The intents kept by a previous visit, or null when there are none or they are unreadable. */
function readKept(value: string | null): KeptIntents | null {
    if (value === null) {
        return null;
    }
    let kept: unknown;
    try {
        kept = JSON.parse(value);
    } catch {
        return null;
    }
    if (
        !isRecord(kept) ||
        typeof kept.token !== 'string' ||
        !isSeq(kept.lastSeq) ||
        !Array.isArray(kept.pending)
    ) {
        return null;
    }
    const pending: PlayerIntentEnvelope[] = [];
    for (const envelope of kept.pending as unknown[]) {
        if (
            !isRecord(envelope) ||
            !isSeq(envelope.clientSeq) ||
            envelope.clientSeq > kept.lastSeq ||
            !isRecord(envelope.intent) ||
            typeof envelope.intent.type !== 'string'
        ) {
            return null;
        }
        // Written by this very page: the server checks the intent anyway.
        pending.push(envelope as unknown as PlayerIntentEnvelope);
    }
    return { token: kept.token, lastSeq: kept.lastSeq, pending };
}

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function isSeq(value: unknown): value is number {
    return typeof value === 'number' && Number.isSafeInteger(value) && value >= 0;
}
