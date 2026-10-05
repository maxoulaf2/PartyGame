<script lang="ts">
    import type { GameMasterViewProps } from '../../shared/modeViews';

    // Until the tracks are played (US-E15-02), the round finishes as soon as it starts: this view
    // only names it, should it ever show.
    let { round }: Pick<GameMasterViewProps, 'round'> = $props();
</script>

<h1>{round.title}</h1>
