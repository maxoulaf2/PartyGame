<script lang="ts">
    import { onMount } from 'svelte';
    import type { PlayerSnapshot } from '../shared/contracts';
    import {
        localCodeStorage,
        playerNicknameKey,
        playerTokenKey,
    } from '../shared/connection/codeStorage';
    import { createGameConnection } from '../shared/connection/gameHub';
    import { PlayerSession } from '../shared/connection/playerSession.svelte';
    import { SnapshotStore } from '../shared/connection/snapshotStore.svelte';
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
{:else}
    <JoinForm {session} />
{/if}
