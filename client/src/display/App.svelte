<script lang="ts">
    import { onMount } from 'svelte';
    import QrCode from '../shared/components/QrCode.svelte';
    import WaitingScreen from '../shared/components/WaitingScreen.svelte';
    import type { DisplaySnapshot } from '../shared/contracts';
    import { connectDisplay } from '../shared/connection/displayConnection';
    import { watchJoinAddress } from '../shared/connection/joinInfo';
    import { composeJoinUrl } from '../shared/connection/joinUrl';
    import { SnapshotStore } from '../shared/connection/snapshotStore.svelte';
    import { countText } from '../shared/i18n/countText';
    import { fr } from '../shared/i18n/fr';

    // undefined until the server first answers, null while it knows no address phones can reach.
    let address = $state<string | null | undefined>(undefined);

    const joinUrl = $derived(address ? composeJoinUrl(address, location) : null);

    const game = new SnapshotStore<DisplaySnapshot>();

    // Until the first snapshot, the TV screen waits as if nobody had joined yet.
    const status = $derived(countText(fr.display.playersJoined, game.current?.playerCount ?? 0));

    onMount(() => {
        const stopWatching = watchJoinAddress((next) => {
            address = next;
        });
        const disconnect = connectDisplay(game);
        return () => {
            stopWatching();
            disconnect();
        };
    });
</script>

{#if joinUrl}
    <main>
        <div class="qr">
            <QrCode text={joinUrl} label={fr.display.qrCodeLabel} />
        </div>
        <div class="details">
            <h1>{fr.app.name}</h1>
            <p class="invite">{fr.display.scanToJoin}</p>
            <p>{fr.display.typeAddress}</p>
            <p class="url">{joinUrl}</p>
            <p class="status">{status}</p>
        </div>
    </main>
{:else}
    <WaitingScreen
        title={fr.app.name}
        message={address === null ? fr.display.joinUnavailable : fr.display.waiting}
    />
{/if}

<style>
    /* TVs may crop their edges (overscan): nothing essential within 5% of any border. */
    main {
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        justify-content: center;
        gap: 5vw;
        min-height: 100vh;
        padding: 5vh 5vw;
    }

    .qr {
        width: min(75vh, 40vw);
    }

    .details {
        display: flex;
        flex-direction: column;
        gap: var(--space-m);
        max-width: 45vw;
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

    .invite {
        font-weight: 700;
    }

    p:not(.invite, .url) {
        color: var(--color-text-muted);
    }

    .url {
        color: var(--color-text);
        font-size: 1.4em;
        font-weight: 700;
        overflow-wrap: anywhere;
    }

    .status {
        margin-top: var(--space-m);
    }
</style>
