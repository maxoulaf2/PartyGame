<script lang="ts">
    import ConfirmDialog from '../shared/components/ConfirmDialog.svelte';
    import type { GameMasterSavedGame } from '../shared/contracts';
    import type { GameMasterSession } from '../shared/connection/gameMasterSession.svelte';
    import { countText } from '../shared/i18n/countText';
    import { fr } from '../shared/i18n/fr';
    import { formatTime } from '../shared/i18n/timeText';
    import { savedGameProgressText } from './savedGameText';

    interface Props {
        /** The game the restarted server found saved. */
        savedGame: GameMasterSavedGame;
        session: GameMasterSession;
        /** Whether the page is synchronized with the server: everything is disabled otherwise. */
        interactive: boolean;
    }

    let { savedGame, session, interactive }: Props = $props();

    const texts = fr.gm.resume;

    let confirming = $state(false);
    let sending = $state(false);
    let failed = $state(false);

    const missing = $derived(savedGame.missingMedia);
    const canAct = $derived(interactive && !sending);

    // Answered once the server handled it: by then the snapshot shows the game resumed, or still the
    // game found if the request was refused, and the request is safe to repeat.
    async function act(request: () => ReturnType<GameMasterSession['checkSavedGameMedia']>) {
        if (!canAct) {
            return;
        }
        sending = true;
        failed = false;
        const outcome = await request();
        sending = false;
        confirming = false;
        failed = outcome === 'unreachable';
    }
</script>

<section aria-labelledby="resume-title">
    <h2 id="resume-title">{texts.title}</h2>
    <p>{texts.intro}</p>
    <dl>
        <dt>{texts.packLabel}</dt>
        <dd>{savedGame.packTitle ?? texts.noPack}</dd>
        <dt>{texts.progressLabel}</dt>
        <dd>{savedGameProgressText(savedGame)}</dd>
        <dt>{texts.playersLabel}</dt>
        <dd>{countText(texts.players, savedGame.playerCount)}</dd>
        <dt>{texts.savedAtLabel}</dt>
        <dd>{formatTime(savedGame.savedAt)}</dd>
    </dl>

    {#if missing.length > 0}
        <div class="problem" role="alert">
            <p>{countText(texts.missingMedia, missing.length)}</p>
            <ul aria-label={texts.missingMediaLabel}>
                {#each missing as media (media)}
                    <!-- Plain text interpolation: a path is never read as HTML. -->
                    <li>{media}</li>
                {/each}
            </ul>
            <button
                type="button"
                class="secondary"
                disabled={!canAct}
                onclick={() => act(() => session.checkSavedGameMedia())}
            >
                {texts.checkAgain}
            </button>
        </div>
    {/if}

    <div class="actions">
        <button
            type="button"
            disabled={!canAct || missing.length > 0}
            onclick={() => act(() => session.resolveSavedGame(savedGame.gameId, true))}
        >
            {texts.resume}
        </button>
        <button
            type="button"
            class="secondary"
            disabled={!canAct}
            onclick={() => {
                failed = false;
                confirming = true;
            }}
        >
            {texts.newGame}
        </button>
    </div>
    {#if failed}
        <p class="failed" role="alert">{texts.failed}</p>
    {/if}
</section>
{#if confirming}
    <ConfirmDialog
        title={texts.confirmTitle}
        message={texts.confirmMessage}
        confirmLabel={texts.confirm}
        cancelLabel={texts.cancel}
        confirmDisabled={!canAct}
        onconfirm={() => act(() => session.resolveSavedGame(savedGame.gameId, false))}
        oncancel={() => (confirming = false)}
    />
{/if}

<style>
    section {
        display: flex;
        flex-direction: column;
        gap: var(--space-m);
    }

    h2,
    p,
    dl,
    dd,
    ul {
        margin: 0;
    }

    h2 {
        font-size: 1.375rem;
    }

    dl {
        display: grid;
        grid-template-columns: auto 1fr;
        gap: var(--space-s) var(--space-m);
        padding: var(--space-m);
        border-radius: var(--radius);
        background: var(--color-surface);
    }

    dt {
        color: var(--color-text-muted);
    }

    dd {
        font-weight: 700;
        overflow-wrap: anywhere;
    }

    .problem {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
        padding: var(--space-m);
        border: 2px solid var(--color-accent);
        border-radius: var(--radius);
    }

    .problem p,
    .failed {
        color: var(--color-accent);
        font-weight: 700;
    }

    ul {
        padding-left: 1.25em;
        overflow-wrap: anywhere;
    }

    .actions {
        display: flex;
        flex-wrap: wrap;
        gap: var(--space-s) var(--space-m);
    }

    button {
        min-height: var(--touch-target-min);
        padding: 0 var(--space-l);
        border: 2px solid var(--color-accent);
        border-radius: var(--radius);
        background: var(--color-accent);
        color: var(--color-bg);
        font: inherit;
        font-size: 1.125rem;
        font-weight: 700;
        cursor: pointer;
        touch-action: manipulation;
    }

    .secondary {
        align-self: flex-start;
        background: transparent;
        color: var(--color-accent);
    }

    button:disabled {
        opacity: 0.4;
        cursor: default;
    }
</style>
