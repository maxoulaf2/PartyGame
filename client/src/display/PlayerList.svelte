<script lang="ts">
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
                    <svg viewBox="0 0 24 24" aria-hidden="true" class="icon">
                        <path
                            d="M2 8.5a15 15 0 0 1 20 0M5.5 12a10 10 0 0 1 13 0M9 15.5a5 5 0 0 1 6 0"
                        />
                        <circle cx="12" cy="19" r="1.2" />
                        <path d="M3 3l18 18" />
                    </svg>
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

    .icon {
        flex: none;
        width: 1em;
        height: 1em;
        fill: currentColor;
        stroke: currentColor;
        stroke-linecap: round;
        stroke-width: 2;
    }

    .icon path {
        fill: none;
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
