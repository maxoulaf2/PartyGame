<script lang="ts">
    import type { GameMasterSnapshot } from '../shared/contracts';
    import { countText } from '../shared/i18n/countText';
    import { fr } from '../shared/i18n/fr';

    interface Props {
        snapshot: GameMasterSnapshot;
    }

    let { snapshot }: Props = $props();
</script>

<main>
    <h1>{fr.gm.consoleTitle}</h1>
    <p>{countText(fr.gm.playersJoined, snapshot.playerCount)}</p>
</main>

<style>
    main {
        display: flex;
        flex-direction: column;
        gap: var(--space-m);
        padding: var(--space-l);
    }

    h1,
    p {
        margin: 0;
    }

    h1 {
        color: var(--color-accent);
        font-size: var(--font-size-title);
    }
</style>
