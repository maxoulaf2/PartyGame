<script lang="ts">
    import WaitingScreen from '../shared/components/WaitingScreen.svelte';
    import { fr } from '../shared/i18n/fr';
</script>

<WaitingScreen title={fr.app.name} message={fr.player.waiting} />
