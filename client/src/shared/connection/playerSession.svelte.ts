import type { JoinRefusal, PlayerSnapshot } from '../contracts';
import type { CodeStorage } from './codeStorage';
import type { GameConnection } from './gameHub';
import type { SnapshotStore } from './snapshotStore.svelte';

/** What became of a nickname the player submitted: joined, refused by the server, or not sent. */
export type JoinOutcome = 'joined' | 'unreachable' | JoinRefusal;

/**
 * The registration of a phone. The player picks a nickname, the server registers them and answers
 * a token, kept on the phone with the nickname. Snapshots go to `store`; the server sends them to
 * this connection once the player is registered.
 */
export class PlayerSession {
    #joined = $state(false);
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
    }

    /** Whether the server registered this phone as a player. */
    get joined(): boolean {
        return this.#joined;
    }

    /** Whether the server can be reached: a nickname cannot be submitted until it can. */
    get connected(): boolean {
        return this.#connected;
    }

    /** The last nickname this phone joined with, to fill the form again, or an empty string. */
    get rememberedNickname(): string {
        return this.#nickname.load() ?? '';
    }

    /** Connects to the server. Returns a function that disconnects. */
    start(): () => void {
        const unsubscribe = this.#connection.on('ReceivePlayerSnapshot', (snapshot) => {
            // The game master may rename the player: the form is filled with the current nickname.
            if (this.#store.accept(snapshot)) {
                this.#nickname.save(snapshot.nickname);
            }
        });
        const lost = () => {
            this.#connected = false;
            this.#store.markStale();
        };
        this.#connection.onReconnecting(lost);
        this.#connection.onClose(lost);
        // Identifying again with the token on the new connection is US-E05-01.
        this.#connection.onReconnected(() => {
            this.#connected = true;
        });
        this.#connection
            .start()
            .then(() => {
                this.#connected = true;
            })
            // An unreachable server leaves the form disabled, never shows an error.
            .catch(() => {});

        return () => {
            unsubscribe();
            this.#connection.stop().catch(() => {});
        };
    }

    /** Sends the nickname the player typed. The server alone decides whether it is valid and free. */
    async join(nickname: string): Promise<JoinOutcome> {
        if (!this.#connected || this.#joined) {
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
        this.#joined = true;
        return 'joined';
    }
}
