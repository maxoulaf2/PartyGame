<script lang="ts">
    import ConnectionIcon from '../shared/components/ConnectionIcon.svelte';
    import type { DisplayPlayer } from '../shared/contracts';
    import { fr } from '../shared/i18n/fr';
    import { playerListLayout } from './playerListLayout';

    interface Props {
        /** In order of arrival, as the server sends them: nobody moves when another one joins. */
        players: readonly DisplayPlayer[];
    }

    let { players }: Props = $props();

    const layout = $derived(playerListLayout(players.length));
</script>

{#if players.length > 0}
    <ul
        aria-label={fr.display.playerListLabel}
        style:--columns={layout.columns}
        style:--nickname-size={layout.fontSize}
    >
        {#each players as player (player.id)}
            <li class:disconnected={!player.isConnected}>
                <!-- Plain text interpolation: Svelte escapes it, so a nickname is never read as HTML. -->
                <span class="nickname">{player.nickname}</span>
                {#if !player.isConnected}
                    <!-- Dimmed and marked with an icon: never told apart by colour alone. -->
                    <ConnectionIcon connected={false} />
                    <span class="visually-hidden">({fr.display.disconnected})</span>
                {/if}
            </li>
        {/each}
    </ul>
{/if}

<style>
    ul {
        display: grid;
        grid-template-columns: repeat(var(--columns), minmax(0, 1fr));
        gap: 0.6vh 1.5vw;
        margin: 0;
        padding: 0;
        font-size: var(--nickname-size);
        line-height: 1.25;
        list-style: none;
    }

    li {
        display: flex;
        align-items: center;
        gap: 0.4em;
        min-width: 0;
        padding: 0.15em 0.5em;
        border-radius: var(--radius);
        background: var(--color-surface);
        font-weight: 700;
    }

    .nickname {
        min-width: 0;
        /* A nickname too wide for its column wraps rather than being cut. */
        overflow-wrap: anywhere;
    }

    .disconnected {
        opacity: 0.45;
    }

    .visually-hidden {
        position: absolute;
        width: 1px;
        height: 1px;
        overflow: hidden;
        clip-path: inset(50%);
        white-space: nowrap;
    }
</style>
