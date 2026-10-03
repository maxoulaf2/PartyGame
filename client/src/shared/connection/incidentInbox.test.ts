import { describe, expect, it } from 'vitest';
import type { Incident, IncidentList } from '../contracts';
import { IncidentInbox } from './incidentInbox.svelte';

function incident(id: number, count = 1): Incident {
    return {
        id,
        code: 'RoundHandlerFailed',
        round: null,
        role: null,
        count,
        lastOccurredAt: 1_790_000_000_000,
    };
}

function list(version: number, ...incidents: Incident[]): IncidentList {
    return { version, incidents };
}

describe('IncidentInbox', () => {
    it('lists nothing and counts nothing before the first list', () => {
        const inbox = new IncidentInbox();

        expect(inbox.incidents).toEqual([]);
        expect(inbox.unread).toBe(0);
    });

    it('counts every occurrence not read, repetitions included', () => {
        const inbox = new IncidentInbox();

        inbox.accept(list(3, incident(2), incident(1, 2)));

        expect(inbox.incidents.map((i) => i.id)).toEqual([2, 1]);
        expect(inbox.unread).toBe(3);
    });

    it('clears the counter until the next incident, and keeps the incidents listed', () => {
        const inbox = new IncidentInbox();
        inbox.accept(list(2, incident(1, 2)));

        inbox.markAllRead();

        expect(inbox.unread).toBe(0);
        expect(inbox.incidents).toHaveLength(1);

        inbox.accept(list(3, incident(1, 3)));

        expect(inbox.unread).toBe(1);
    });

    it('ignores an older list, or the same one again', () => {
        const inbox = new IncidentInbox();
        inbox.accept(list(2, incident(2), incident(1)));

        expect(inbox.accept(list(1, incident(1)))).toBe(false);
        expect(inbox.accept(list(2, incident(2), incident(1)))).toBe(false);
        expect(inbox.incidents).toHaveLength(2);
    });

    it('takes the list of a restarted server after a lost connection, none of it read', () => {
        const inbox = new IncidentInbox();
        inbox.accept(list(5, incident(1, 5)));
        inbox.markAllRead();

        inbox.markStale();
        const kept = inbox.accept(list(1, incident(1)));

        expect(kept).toBe(true);
        expect(inbox.incidents).toEqual([incident(1)]);
        expect(inbox.unread).toBe(1);
    });

    it('keeps what was read when the same server sends its list again after a lost connection', () => {
        const inbox = new IncidentInbox();
        inbox.accept(list(2, incident(1, 2)));
        inbox.markAllRead();

        inbox.markStale();
        inbox.accept(list(2, incident(1, 2)));

        expect(inbox.unread).toBe(0);
    });
});
