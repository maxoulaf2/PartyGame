<script lang="ts">
    import type { PlayerSnapshot } from '../shared/contracts';
    import { fr } from '../shared/i18n/fr';

    interface Props {
        snapshot: PlayerSnapshot;
    }

    let { snapshot }: Props = $props();

    // A function as replacement: a nickname such as « $& » must show as typed.
    const registeredAs = $derived(
        fr.player.registeredAs.replace('{nickname}', () => snapshot.nickname),
    );
</script>

<main>
    <h1>{fr.app.name}</h1>
    <p class="nickname">{registeredAs}</p>
    <p class="status">{fr.player.waiting}</p>
</main>

<style>
    main {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: var(--space-m);
        min-height: 100vh;
        min-height: 100dvh;
        padding: var(--space-l);
        text-align: center;
    }

    h1,
    p {
        margin: 0;
    }

    h1 {
        color: var(--color-accent);
        font-size: var(--font-size-title);
    }

    .nickname {
        font-size: 1.25rem;
        font-weight: 700;
        overflow-wrap: anywhere;
    }

    .status {
        color: var(--color-text-muted);
    }
</style>
