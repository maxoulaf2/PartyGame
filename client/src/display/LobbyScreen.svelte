<script lang="ts">
    import QrCode from '../shared/components/QrCode.svelte';
    import type { DisplaySnapshot } from '../shared/contracts';
    import { composeJoinUrl } from '../shared/connection/joinUrl';
    import { countText } from '../shared/i18n/countText';
    import { fr } from '../shared/i18n/fr';
    import PlayerList from './PlayerList.svelte';

    interface Props {
        snapshot: DisplaySnapshot;
    }

    let { snapshot }: Props = $props();

    const joinUrl = $derived(
        snapshot.joinAddress ? composeJoinUrl(snapshot.joinAddress, location) : null,
    );
    const status = $derived(countText(fr.display.playersJoined, snapshot.players.length));
    // Registration stays open once started: the QR code and the list stay for late arrivals.
    const started = $derived(snapshot.phase === 'Started');
</script>

<main>
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
        <h1>{fr.app.name}</h1>
        {#if started}
            <p class="started">{fr.display.started}</p>
        {/if}
        <p class="status">{status}</p>
        <PlayerList players={snapshot.players} />
    </section>
</main>

<style>
    /* TVs may crop their edges (overscan): nothing essential within 5% of any border. */
    main {
        display: flex;
        align-items: center;
        gap: 4vw;
        height: 100vh;
        padding: 5vh 5vw;
        overflow: hidden;
    }

    .join {
        display: flex;
        flex: 0 0 34vw;
        flex-direction: column;
        align-items: center;
        gap: var(--space-m);
        text-align: center;
    }

    .qr {
        width: min(55vh, 30vw);
    }

    .lobby {
        display: flex;
        flex: 1;
        flex-direction: column;
        gap: var(--space-m);
        align-self: stretch;
        justify-content: center;
        min-width: 0;
    }

    h1,
    p {
        margin: 0;
    }

    h1 {
        color: var(--color-accent);
        font-size: var(--font-size-title);
        line-height: 1.1;
    }

    .invite,
    .url {
        font-weight: 700;
    }

    .muted {
        color: var(--color-text-muted);
    }

    .url {
        overflow-wrap: anywhere;
    }

    .status {
        color: var(--color-text-muted);
    }

    .started {
        font-size: var(--font-size-title);
        font-weight: 700;
        line-height: 1.1;
    }
</style>
