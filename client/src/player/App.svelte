<script lang="ts">
    import { onMount } from 'svelte';
    import WaitingScreen from '../shared/components/WaitingScreen.svelte';
    import type { PlayerSnapshot } from '../shared/contracts';
    import {
        localCodeStorage,
        playerNicknameKey,
        playerTokenKey,
    } from '../shared/connection/codeStorage';
    import { createGameConnection } from '../shared/connection/gameHub';
    import { PlayerSession } from '../shared/connection/playerSession.svelte';
    import { SnapshotStore } from '../shared/connection/snapshotStore.svelte';
    import { fr } from '../shared/i18n/fr';
    import JoinForm from './JoinForm.svelte';
    import LobbyScreen from './LobbyScreen.svelte';

    const game = new SnapshotStore<PlayerSnapshot>();
    const session = new PlayerSession(
        game,
        localCodeStorage(playerTokenKey),
        localCodeStorage(playerNicknameKey),
        createGameConnection(),
    );

    onMount(() => session.start());
</script>

{#if session.joined && game.current}
    <LobbyScreen snapshot={game.current} />
{:else if session.status !== 'registering'}
    <!-- A phone that joined before waits for the server to recognize it, never on the form. -->
    <WaitingScreen title={fr.app.name} message={fr.player.resuming} />
{:else}
    <JoinForm {session} />
{/if}
