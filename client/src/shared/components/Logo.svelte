<script lang="ts">
    import { fr } from '../i18n/fr';

    interface Props {
        /** Side by side on the TV screen, else stacked as on a phone. */
        tv?: boolean;
    }

    let { tv = false }: Props = $props();
</script>

<!-- Two stickers, read as the name of the game. -->
<h1 class:tv aria-label={fr.app.name}>
    <span class="first" aria-hidden="true">{fr.app.logo.first}</span>
    <span class="second" aria-hidden="true">{fr.app.logo.second}</span>
</h1>

<style>
    h1 {
        display: flex;
        flex-direction: column;
        align-items: flex-start;
        gap: 6px;
        margin: 0;
        color: var(--color-ink);
        font-size: 76px;
        font-weight: 800;
        line-height: 1;
        letter-spacing: -0.03em;
    }

    span {
        padding: 0.05em 0.26em 0.13em;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: 0.29em;
        box-shadow: 0 0.09em 0 var(--color-ink);
    }

    .first {
        background: var(--color-pink);
        transform: rotate(-4deg);
    }

    .second {
        margin-left: 0.95em;
        background: var(--color-accent);
        transform: rotate(3deg);
    }

    .tv {
        flex-direction: row;
        gap: calc(10 * var(--u));
        font-size: calc(58 * var(--u));
    }

    .tv .first {
        transform: rotate(-3deg);
    }

    .tv .second {
        margin: 0.24em 0 0;
        transform: rotate(2deg);
    }
</style>
