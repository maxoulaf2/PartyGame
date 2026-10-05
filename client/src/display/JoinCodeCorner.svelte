<script lang="ts">
    import QrCode from '../shared/components/QrCode.svelte';
    import { composeJoinUrl } from '../shared/connection/joinUrl';
    import { fr } from '../shared/i18n/fr';

    interface Props {
        /** The address phones join at, as in the snapshot. */
        joinAddress: string;
    }

    let { joinAddress }: Props = $props();

    const joinUrl = $derived(composeJoinUrl(joinAddress, location));
</script>

<!-- Over the game, in a corner: the screen underneath goes on as if it were not there. -->
<aside>
    <div class="qr">
        <QrCode text={joinUrl} label={fr.display.qrCodeLabel} />
    </div>
    <p>{fr.display.scanToJoin}</p>
</aside>

<style>
    /* TVs may crop their edges (overscan): kept within the 5% margin of the TV screen layout. */
    aside {
        position: fixed;
        top: 5vh;
        left: 5vw;
        z-index: 1;
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: var(--space-s);
        width: 16vh;
        padding: var(--space-s);
        border-radius: var(--radius);
        background: var(--color-surface);
        pointer-events: none;
    }

    .qr {
        width: 100%;
    }

    p {
        margin: 0;
        font-size: 0.4em;
        font-weight: 700;
        text-align: center;
    }
</style>
