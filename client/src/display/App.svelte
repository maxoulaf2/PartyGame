<script lang="ts">
    import { onMount } from 'svelte';
    import WaitingScreen from '../shared/components/WaitingScreen.svelte';
    import { fetchJoinInfo } from '../shared/connection/joinInfo';
    import { fr } from '../shared/i18n/fr';

    // Until the server confirms it has no address, the screen keeps the usual waiting text.
    let hasAddress = $state(true);

    onMount(() => {
        void fetchJoinInfo().then((info) => {
            hasAddress = info?.address != null;
        });
    });
</script>

<WaitingScreen
    title={fr.app.name}
    message={hasAddress ? fr.display.waiting : fr.display.joinUnavailable}
/>
