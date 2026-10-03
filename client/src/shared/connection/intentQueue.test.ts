import { describe, expect, it, vi } from 'vitest';
import type { PlayerIntentEnvelope, RoundId } from '../contracts';
import type { CodeStorage } from './codeStorage';
import { IntentQueue } from './intentQueue.svelte';

const token = 'q1w2e3r4t5y6u7i8o9p0aa';
const roundId = '0f8fad5b-d9cb-469f-a165-70867728950e' as RoundId;
const answer = { type: 'quiz.submitAnswer', roundId, questionNumber: 1, choice: 'A' } as const;
const second = { ...answer, questionNumber: 2, choice: 'B' } as const;

function memoryStorage(initial: string | null = null): CodeStorage & { value: string | null } {
    const storage = {
        value: initial,
        load: () => storage.value,
        save: (value: string) => {
            storage.value = value;
        },
        clear: () => {
            storage.value = null;
        },
    };
    return storage;
}

/** A sender whose acknowledgments the test gives, or withholds, one by one. */
function manualSender() {
    const calls: { envelope: PlayerIntentEnvelope; ack: () => void; lose: () => void }[] = [];
    const send = vi.fn(
        (envelope: PlayerIntentEnvelope) =>
            new Promise<void>((resolve, reject) => {
                calls.push({ envelope, ack: resolve, lose: () => reject(new Error('lost')) });
            }),
    );
    return { send, calls, envelopes: () => calls.map((call) => call.envelope) };
}

describe('IntentQueue', () => {
    it('numbers the intents from 1 and sends them one after the other', async () => {
        const queue = new IntentQueue(memoryStorage());
        queue.reset(token);
        const sender = manualSender();
        queue.open(sender.send);

        queue.enqueue(answer);
        queue.enqueue(second);

        // The second leaves only once the first is acknowledged, to keep the order.
        expect(sender.envelopes()).toEqual([{ clientSeq: 1, intent: answer }]);
        sender.calls[0]?.ack();
        await vi.waitFor(() => expect(sender.calls).toHaveLength(2));
        expect(sender.envelopes()[1]).toEqual({ clientSeq: 2, intent: second });
        sender.calls[1]?.ack();
        await vi.waitFor(() => expect(queue.pending).toEqual([]));
    });

    it('keeps the intents while closed, then sends them once opened', async () => {
        const queue = new IntentQueue(memoryStorage());
        queue.reset(token);

        queue.enqueue(answer);
        expect(queue.pending).toEqual([answer]);
        const sender = manualSender();
        queue.open(sender.send);

        expect(sender.envelopes()).toEqual([{ clientSeq: 1, intent: answer }]);
        sender.calls[0]?.ack();
        await vi.waitFor(() => expect(queue.pending).toEqual([]));
    });

    it('keeps an intent whose acknowledgment is lost, and sends it again once opened anew', async () => {
        const queue = new IntentQueue(memoryStorage());
        queue.reset(token);
        const lost = manualSender();
        queue.open(lost.send);
        queue.enqueue(answer);

        queue.close();
        lost.calls[0]?.lose();
        await Promise.resolve();
        expect(queue.pending).toEqual([answer]);
        const restored = manualSender();
        queue.open(restored.send);

        await vi.waitFor(() =>
            expect(restored.envelopes()).toEqual([{ clientSeq: 1, intent: answer }]),
        );
    });

    it('sends again through a new connection opened before the old one gave up', async () => {
        const queue = new IntentQueue(memoryStorage());
        queue.reset(token);
        const lost = manualSender();
        queue.open(lost.send);
        queue.enqueue(answer);

        queue.close();
        const restored = manualSender();
        queue.open(restored.send);
        expect(restored.calls).toHaveLength(0);
        lost.calls[0]?.lose();

        await vi.waitFor(() =>
            expect(restored.envelopes()).toEqual([{ clientSeq: 1, intent: answer }]),
        );
    });

    it('drops an intent acknowledged after its connection was lost', async () => {
        const queue = new IntentQueue(memoryStorage());
        queue.reset(token);
        const sender = manualSender();
        queue.open(sender.send);
        queue.enqueue(answer);

        queue.close();
        sender.calls[0]?.ack();

        await vi.waitFor(() => expect(queue.pending).toEqual([]));
    });

    it('stops sending when the sender fails, until opened again', async () => {
        const queue = new IntentQueue(memoryStorage());
        queue.reset(token);
        const send = vi.fn(() => Promise.reject(new Error('lost')));
        queue.open(send);

        queue.enqueue(answer);
        await vi.waitFor(() => expect(send).toHaveBeenCalledOnce());
        queue.enqueue(second);

        await Promise.resolve();
        expect(send).toHaveBeenCalledOnce();
        expect(queue.pending).toEqual([answer, second]);
    });

    it('ignores intents without a token', () => {
        const storage = memoryStorage();
        const queue = new IntentQueue(storage);
        const send = vi.fn(() => Promise.resolve());
        queue.open(send);

        queue.enqueue(answer);

        expect(queue.pending).toEqual([]);
        expect(send).not.toHaveBeenCalled();
        expect(storage.value).toBeNull();
    });

    describe('kept between visits', () => {
        it('takes up the pending intents and the numbers kept for the same token', async () => {
            const storage = memoryStorage();
            const before = new IntentQueue(storage);
            before.reset(token);
            before.enqueue(answer);

            const queue = new IntentQueue(storage);
            queue.restore(token);
            expect(queue.pending).toEqual([answer]);
            queue.enqueue(second);
            const sender = manualSender();
            queue.open(sender.send);
            sender.calls[0]?.ack();

            await vi.waitFor(() => expect(sender.calls).toHaveLength(2));
            expect(sender.envelopes()).toEqual([
                { clientSeq: 1, intent: answer },
                { clientSeq: 2, intent: second },
            ]);
        });

        it('keeps counting after the acknowledged intents', async () => {
            const storage = memoryStorage();
            const before = new IntentQueue(storage);
            before.reset(token);
            before.open(() => Promise.resolve());
            before.enqueue(answer);
            await vi.waitFor(() => expect(before.pending).toEqual([]));

            const queue = new IntentQueue(storage);
            queue.restore(token);
            const sender = manualSender();
            queue.open(sender.send);
            queue.enqueue(second);

            expect(sender.envelopes()).toEqual([{ clientSeq: 2, intent: second }]);
        });

        it('forgets the intents of another token', () => {
            const storage = memoryStorage();
            const before = new IntentQueue(storage);
            before.reset('another-token');
            before.enqueue(answer);

            const queue = new IntentQueue(storage);
            queue.restore(token);
            const sender = manualSender();
            queue.open(sender.send);
            queue.enqueue(second);

            expect(sender.envelopes()).toEqual([{ clientSeq: 1, intent: second }]);
        });

        it.each([
            ['not JSON', '{'],
            ['not an object', '[]'],
            ['without a token', JSON.stringify({ lastSeq: 1, pending: [] })],
            ['with a broken number', JSON.stringify({ token, lastSeq: -1, pending: [] })],
            [
                'with an intent numbered after the last number',
                JSON.stringify({ token, lastSeq: 1, pending: [{ clientSeq: 2, intent: answer }] }),
            ],
            [
                'with an intent without type',
                JSON.stringify({ token, lastSeq: 1, pending: [{ clientSeq: 1, intent: {} }] }),
            ],
        ])('starts afresh from a kept value %s', (_, value) => {
            const queue = new IntentQueue(memoryStorage(value));

            queue.restore(token);

            expect(queue.pending).toEqual([]);
        });

        it('forgets everything once cleared', () => {
            const storage = memoryStorage();
            const queue = new IntentQueue(storage);
            queue.reset(token);
            queue.enqueue(answer);

            queue.clear();

            expect(queue.pending).toEqual([]);
            expect(storage.value).toBeNull();
        });

        it('starts the numbers over for a new token', () => {
            const storage = memoryStorage();
            const queue = new IntentQueue(storage);
            queue.reset('another-token');
            queue.enqueue(answer);

            queue.reset(token);
            const sender = manualSender();
            queue.open(sender.send);
            queue.enqueue(second);

            expect(sender.envelopes()).toEqual([{ clientSeq: 1, intent: second }]);
        });
    });
});
