<script lang="ts">
    import Confetti from '../shared/components/Confetti.svelte';
    import Logo from '../shared/components/Logo.svelte';
    import QrCode from '../shared/components/QrCode.svelte';
    import type { DisplaySnapshot } from '../shared/contracts';
    import { composeJoinUrl } from '../shared/connection/joinUrl';
    import { countText } from '../shared/i18n/countText';
    import { fr } from '../shared/i18n/fr';
    import PlayerList from './PlayerList.svelte';

    interface Props {
        snapshot: DisplaySnapshot;
        /** What is going on, once the game has started. */
        notice?: string | null;
    }

    let { snapshot, notice = null }: Props = $props();

    const joinUrl = $derived(
        snapshot.joinAddress ? composeJoinUrl(snapshot.joinAddress, location) : null,
    );
    const status = $derived(countText(fr.display.playersJoined, snapshot.players.length));
    // A function as replacement: a title such as « $& » must show as written in the pack.
    const packTitle = $derived(
        snapshot.packTitle === null
            ? null
            : fr.display.packTitle.replace('{title}', () => snapshot.packTitle ?? ''),
    );
</script>

<!-- Beyond 12 players, the stickers slim down so that 20 long nicknames still fit. -->
<main class:dense={snapshot.players.length > 12}>
    <Confetti tv />
    <section class="join">
        {#if joinUrl}
            <p class="invite">{fr.display.scanToJoin}</p>
            <div class="qr">
                <QrCode text={joinUrl} label={fr.display.qrCodeLabel} />
            </div>
            <p class="muted">{fr.display.typeAddress}</p>
            <p class="url">{joinUrl}</p>
        {:else}
            <!-- The server knows no address phones can reach: a neutral message, never an error. -->
            <p class="muted">{fr.display.joinUnavailable}</p>
        {/if}
    </section>
    <section class="lobby">
        <Logo tv />
        {#if packTitle !== null}
            <p class="pack">{packTitle}</p>
        {/if}
        {#if notice !== null}
            <p class="headline">{notice}</p>
        {/if}
        <p class="status">{status}</p>
        <PlayerList players={snapshot.players} />
    </section>
</main>

<style>
    /* TVs may crop their edges (overscan): nothing essential within 5% of any border. */
    main {
        position: relative;
        isolation: isolate;
        display: flex;
        align-items: center;
        gap: calc(44 * var(--u));
        height: 100vh;
        padding: 5vh 5vw;
        overflow: hidden;
    }

    .join {
        display: flex;
        flex: 0 0 calc(300 * var(--u));
        flex-direction: column;
        align-items: center;
        gap: calc(12 * var(--u));
        padding: calc(22 * var(--u));
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: calc(26 * var(--u));
        background: var(--color-surface);
        box-shadow: 0 calc(8 * var(--u)) 0 var(--color-ink);
        color: var(--color-on-surface);
        text-align: center;
        transform: rotate(-2deg);
    }

    .qr {
        width: calc(220 * var(--u));
        overflow: hidden;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: calc(14 * var(--u));
    }

    .lobby {
        display: flex;
        flex: 1;
        flex-direction: column;
        gap: calc(14 * var(--u));
        align-self: stretch;
        justify-content: center;
        min-width: 0;
    }

    p {
        margin: 0;
    }

    .invite {
        font-size: calc(21 * var(--u));
        font-weight: 800;
        line-height: 1.15;
    }

    .muted {
        color: var(--color-on-surface-muted);
        font-size: calc(14 * var(--u));
        font-weight: 600;
    }

    .url {
        padding: calc(2 * var(--u)) calc(10 * var(--u));
        border: calc(2 * var(--u)) solid var(--color-ink);
        border-radius: calc(10 * var(--u));
        background: var(--color-accent);
        color: var(--color-ink);
        font-size: calc(18 * var(--u));
        font-weight: 800;
        overflow-wrap: anywhere;
    }

    .pack {
        margin-top: calc(8 * var(--u));
        font-size: calc(24 * var(--u));
        font-weight: 800;
        overflow-wrap: anywhere;
    }

    .headline {
        font-size: calc(44 * var(--u));
        font-weight: 800;
        line-height: 1.1;
        -webkit-text-stroke: calc(1.5 * var(--u)) var(--color-ink);
    }

    .status {
        align-self: flex-start;
        padding: calc(5 * var(--u)) calc(14 * var(--u));
        border-radius: 999px;
        background: var(--color-ink);
        color: var(--color-surface);
        font-size: calc(16 * var(--u));
        font-weight: 700;
    }

    .dense {
        --sticker-line: calc(2 * var(--u));
        --player-gap: 0.6vh 1.5vw;
    }

    .dense .lobby {
        gap: 1.5vh;
    }

    .dense .lobby > :global(h1) {
        font-size: calc(40 * var(--u));
    }

    .dense .pack {
        margin-top: 0;
    }
</style>
