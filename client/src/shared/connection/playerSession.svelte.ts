import type { JoinRefusal, PlayerSnapshot } from '../contracts';
import type { CodeStorage } from './codeStorage';
import type { GameConnection } from './gameHub';
import type { SnapshotStore } from './snapshotStore.svelte';

/** What became of a nickname the player submitted: joined, refused by the server, or not sent. */
export type JoinOutcome = 'joined' | 'unreachable' | JoinRefusal;

/**
 * Where the phone stands:
 * - `registering`: the player has no token, or the server no longer knows it: the form is shown;
 * - `resuming`: a token is kept, and the server has not recognized it yet;
 * - `joined`: the server registered or recognized the player and sends their snapshots.
 */
export type PlayerStatus = 'registering' | 'resuming' | 'joined';

/**
 * The identity of a phone. The player picks a nickname once, the server registers them and
 * answers a token, kept on the phone with the nickname. At every connection afterwards, including
 * after a reload, a sleep or a lost network, the phone presents the token to be recognized as the
 * same player. Snapshots go to `store`; the server sends them once the player is identified.
 */
export class PlayerSession {
    #status = $state<PlayerStatus>('registering');
    #connected = $state(false);

    readonly #store: SnapshotStore<PlayerSnapshot>;
    readonly #token: CodeStorage;
    readonly #nickname: CodeStorage;
    readonly #connection: GameConnection;

    constructor(
        store: SnapshotStore<PlayerSnapshot>,
        token: CodeStorage,
        nickname: CodeStorage,
        connection: GameConnection,
    ) {
        this.#store = store;
        this.#token = token;
        this.#nickname = nickname;
        this.#connection = connection;
        // A kept token is presented without showing the form, which would only flash.
        this.#status = token.load() === null ? 'registering' : 'resuming';
    }

    get status(): PlayerStatus {
        return this.#status;
    }

    /** Whether the server registered or recognized this phone as a player. */
    get joined(): boolean {
        return this.#status === 'joined';
    }

    /** Whether the server can be reached: a nickname cannot be submitted until it can. */
    get connected(): boolean {
        return this.#connected;
    }

    /**
     * Whether the page shows the state of the server since the connection was last established:
     * connected, and either on the form, which waits for no snapshot, or with a fresh snapshot.
     */
    get synchronized(): boolean {
        return this.#connected && (this.#status === 'registering' || this.#store.fresh);
    }

    /** The last nickname this phone joined with, to fill the form again, or an empty string. */
    get rememberedNickname(): string {
        return this.#nickname.load() ?? '';
    }

    /** Connects to the server and presents the kept token, if any. Returns a function that disconnects. */
    start(): () => void {
        const unsubscribe = this.#connection.on('ReceivePlayerSnapshot', (snapshot) => {
            // The game master may rename the player: the form is filled with the current nickname.
            if (this.#store.accept(snapshot)) {
                this.#nickname.save(snapshot.nickname);
            }
        });
        this.#connection.onReconnecting(() => {
            this.#connected = false;
            this.#store.markStale();
        });
        this.#connection.onReconnected(() => this.#onConnected());
        this.#connection
            .start()
            .then(() => this.#onConnected())
            // Only a stop ends the attempts: nothing left to show.
            .catch(() => {});

        return () => {
            unsubscribe();
            this.#connection.stop().catch(() => {});
        };
    }

    /** Sends the nickname the player typed. The server alone decides whether it is valid and free. */
    async join(nickname: string): Promise<JoinOutcome> {
        if (!this.#connected || this.#status !== 'registering') {
            return 'unreachable';
        }

        let result;
        try {
            result = await this.#connection.invoke('JoinGame', { nickname });
        } catch {
            return 'unreachable';
        }

        if (result.refusal !== null) {
            return result.refusal;
        }
        if (result.token === null) {
            // A registration without a token could never be resumed: treat it as a failure.
            return 'JoinFailed';
        }

        this.#token.save(result.token);
        this.#nickname.save(nickname.trim());
        this.#status = 'joined';
        return 'joined';
    }

    #onConnected(): void {
        this.#connected = true;
        const token = this.#token.load();
        if (token !== null) {
            void this.#resume(token);
        }
    }

    async #resume(token: string): Promise<void> {
        let refusal;
        try {
            ({ refusal } = await this.#connection.invoke('ResumeSession', { token }));
        } catch {
            // The connection dropped meanwhile: the token is presented again when it is back.
            return;
        }

        if (refusal === null) {
            this.#status = 'joined';
        } else if (refusal === 'SessionUnknown') {
            // A restarted server or an old evening: the player registers again, nickname filled.
            this.#token.clear();
            this.#status = 'registering';
        }
        // Any other refusal is transient: the token is presented again at the next connection.
    }
}
