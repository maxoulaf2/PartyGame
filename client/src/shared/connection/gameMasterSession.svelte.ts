import type { GameMasterSnapshot, PlayerId, RenamePlayerRefusal } from '../contracts';
import type { CodeStorage } from './codeStorage';
import type { GameConnection } from './gameHub';
import type { SnapshotStore } from './snapshotStore.svelte';

/**
 * Where the game master stands:
 * - `codeRequired`: the console waits for the code to be typed;
 * - `checking`: the remembered code is being presented, and the server has not answered yet;
 * - `granted`: the server accepted the code and sends the `GameMaster` snapshots.
 */
export type GameMasterAccess = 'codeRequired' | 'checking' | 'granted';

/**
 * Why the code is asked for again: `invalid` for a wrong code just typed, `expired` for a
 * remembered code the server no longer accepts (it restarted with a new one).
 */
export type CodeProblem = 'invalid' | 'expired';

/** What became of a code the game master submitted. */
export type SubmitOutcome = 'granted' | 'refused' | 'unreachable';

/**
 * What became of a rename the game master asked for: done, refused by the server, or not sent
 * (connection lost, or the console no longer authenticated).
 */
export type RenameOutcome = 'renamed' | 'unreachable' | RenamePlayerRefusal;

// Six ASCII digits, like `GameMasterCode` on the server.
const completeCode = /^[0-9]{6}$/;

/** Whether `code` can be sent: 6 digits, spaces around tolerated (the server trims them too). */
export function isCodeComplete(code: string): boolean {
    return completeCode.test(code.trim());
}

/**
 * The authentication of the GM console. The code is typed once, then remembered on the device and
 * presented at every connection, including after a reload or a lost connection. Snapshots go to
 * `store`; the server only sends them once the code is accepted.
 */
export class GameMasterSession {
    #access = $state<GameMasterAccess>('codeRequired');
    #problem = $state<CodeProblem | null>(null);
    #connected = $state(false);

    readonly #store: SnapshotStore<GameMasterSnapshot>;
    readonly #storage: CodeStorage;
    readonly #connection: GameConnection;

    constructor(
        store: SnapshotStore<GameMasterSnapshot>,
        storage: CodeStorage,
        connection: GameConnection,
    ) {
        this.#store = store;
        this.#storage = storage;
        this.#connection = connection;
        // A remembered code is checked without showing the form, which would only flash.
        this.#access = storage.load() === null ? 'codeRequired' : 'checking';
    }

    get access(): GameMasterAccess {
        return this.#access;
    }

    get problem(): CodeProblem | null {
        return this.#problem;
    }

    /** Whether the server can be reached: a code cannot be submitted until it can. */
    get connected(): boolean {
        return this.#connected;
    }

    /** Connects to the server and presents the remembered code, if any. Returns a function that disconnects. */
    start(): () => void {
        const unsubscribe = this.#connection.on('ReceiveGameMasterSnapshot', (snapshot) => {
            this.#store.accept(snapshot);
        });
        this.#connection.onReconnecting(() => {
            this.#connected = false;
            this.#store.markStale();
        });
        this.#connection.onClose(() => {
            this.#connected = false;
            this.#store.markStale();
        });
        this.#connection.onReconnected(() => this.#onConnected());
        this.#connection
            .start()
            .then(() => this.#onConnected())
            // An unreachable server leaves the form disabled, never shows an error.
            .catch(() => {});

        return () => {
            unsubscribe();
            this.#connection.stop().catch(() => {});
        };
    }

    /** Sends a code typed by the game master. Remembered only once the server accepts it. */
    async submit(code: string): Promise<SubmitOutcome> {
        if (!this.#connected || !isCodeComplete(code) || this.#access !== 'codeRequired') {
            return 'unreachable';
        }
        return this.#announce(code.trim(), false);
    }

    /**
     * Renames a player. The server alone decides whether the nickname is valid and free; the new
     * nickname reaches the console through the next snapshot.
     */
    async rename(playerId: PlayerId, nickname: string): Promise<RenameOutcome> {
        if (!this.#connected || this.#access !== 'granted') {
            return 'unreachable';
        }

        let result;
        try {
            result = await this.#connection.invoke('RenamePlayer', { playerId, nickname });
        } catch {
            return 'unreachable';
        }
        // No answer: the server ignored the intent, the code is being checked again.
        if (result === null) {
            return 'unreachable';
        }
        return result.refusal ?? 'renamed';
    }

    #onConnected(): void {
        this.#connected = true;
        const remembered = this.#storage.load();
        if (remembered !== null) {
            void this.#announce(remembered, true);
        }
    }

    async #announce(code: string, remembered: boolean): Promise<SubmitOutcome> {
        const previous = this.#access;
        // Only a remembered code is checked out of sight. A typed code keeps the form on screen,
        // so that a refusal can clear and focus the very field the game master typed in, and a
        // console already shown stays shown while a reconnection checks the code again.
        if (remembered && previous === 'codeRequired') {
            this.#access = 'checking';
        }
        let refusal;
        try {
            ({ refusal } = await this.#connection.invoke('Announce', {
                role: 'GameMaster',
                gameMasterCode: code,
            }));
        } catch {
            // The connection dropped meanwhile: the remembered code is presented again when it is back.
            this.#access = remembered ? previous : 'codeRequired';
            return 'unreachable';
        }

        if (refusal === null) {
            this.#storage.save(code);
            this.#problem = null;
            this.#access = 'granted';
            return 'granted';
        }

        if (remembered) {
            this.#storage.clear();
        }
        this.#problem = remembered ? 'expired' : 'invalid';
        this.#access = 'codeRequired';
        return 'refused';
    }
}
