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

    // A sticker each, of a color and a tilt of its own: taken in turn in the order of arrival, so
    // that nobody's sticker changes when another one joins. Never the only way to tell them apart.
    const colors = [
        'var(--color-pink)',
        'var(--color-blue)',
        'var(--color-accent)',
        'var(--color-green)',
        'var(--color-orange)',
        'var(--color-lilac)',
        'var(--color-surface)',
        'var(--color-rose)',
    ];
    const tilts = [-2, 1.5, -1, 2, -1.5, 1, -2.5, 1.5];
</script>

{#if players.length > 0}
    <ul
        aria-label={fr.display.playerListLabel}
        style:--columns={layout.columns}
        style:--nickname-size={layout.fontSize}
    >
        {#each players as player, index (player.id)}
            <li
                class:disconnected={!player.isConnected}
                style:background={colors[index % colors.length]}
                style:transform="rotate({tilts[index % tilts.length]}deg)"
            >
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
        /* Tighter when the lobby fills up. */
        gap: var(--player-gap, calc(10 * var(--u)));
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
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: calc(14 * var(--u));
        box-shadow: 0 calc(4 * var(--u)) 0 var(--color-ink);
        color: var(--color-ink);
        font-weight: 800;
    }

    .nickname {
        min-width: 0;
        /* A nickname too wide for its column wraps rather than being cut. */
        overflow-wrap: anywhere;
    }

    .disconnected {
        opacity: 0.5;
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
