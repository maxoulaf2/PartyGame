<script lang="ts">
    import ConfirmDialog from '../shared/components/ConfirmDialog.svelte';
    import type { GameMasterPack, GameMasterSnapshot } from '../shared/contracts';
    import type { GameMasterSession } from '../shared/connection/gameMasterSession.svelte';
    import { countText } from '../shared/i18n/countText';
    import { fill } from '../shared/i18n/fill';
    import { fr } from '../shared/i18n/fr';
    import { describeMode, describeProblem } from './packProblemText';

    interface Props {
        snapshot: GameMasterSnapshot;
        session: GameMasterSession;
        /** Whether the page is synchronized with the server: everything is disabled otherwise. */
        interactive: boolean;
    }

    let { snapshot, session, interactive }: Props = $props();

    // The pack just chosen, shown until the snapshot selects it or the server refuses it: the
    // snapshot has the last word.
    let pending = $state<string | null>(null);
    let selectFailed = $state(false);
    let reloading = $state(false);
    let reloadFailed = $state(false);
    // The pack whose preview waits for the game master to confirm that answers will show.
    let previewing = $state<string | null>(null);

    // A derived primitive only changes with the selection itself, not with every snapshot.
    const selected = $derived(snapshot.selectedPackId);
    const catalog = $derived(snapshot.packCatalog);
    const chosen = $derived(pending ?? selected);
    const canChoose = $derived(interactive && pending === null && !reloading);

    $effect(() => {
        void selected;
        pending = null;
    });

    function stateOf(pack: GameMasterPack): string {
        return pack.isValid
            ? fr.gm.packs.valid
            : countText(fr.gm.packs.invalid, pack.problems.length);
    }

    async function choose(packId: string) {
        if (!canChoose || packId === selected) {
            return;
        }
        selectFailed = false;
        pending = packId;
        const outcome = await session.selectPack(packId);
        if (outcome !== 'selected') {
            // Unknown, invalid, or a lost connection: the snapshot shows what is selected.
            pending = null;
            selectFailed = outcome === 'SelectionFailed';
        }
    }

    function preview() {
        if (previewing !== null) {
            void session.startPreview(previewing);
        }
        previewing = null;
    }

    async function reload() {
        if (!interactive || reloading) {
            return;
        }
        reloadFailed = false;
        reloading = true;
        const outcome = await session.reloadPacks();
        reloading = false;
        // Already started: the snapshot shows it. A lost connection is for the connection
        // indicator to show.
        reloadFailed = outcome === 'ReloadFailed';
    }
</script>

<section class="packs" aria-labelledby="packs-title">
    <h2 id="packs-title">{fr.gm.packs.title}</h2>
    {#if catalog === null}
        {#if snapshot.packTitle !== null}
            <p class="played">{fill(fr.gm.packs.played, { title: snapshot.packTitle })}</p>
        {/if}
    {:else}
        <p class="hint directory">
            {fill(fr.gm.packs.directory, { directory: catalog.directory })}
        </p>
        {#if catalog.packs.length === 0}
            <p class="none">{fr.gm.packs.none}</p>
            <p class="hint">{fr.gm.packs.noneHint}</p>
        {:else}
            <ul aria-label={fr.gm.packs.listLabel}>
                {#each catalog.packs as pack, index (pack.id)}
                    <li class:chosen={chosen === pack.id} class:invalid={!pack.isValid}>
                        <div class="choice">
                            <input
                                type="radio"
                                name="pack"
                                id="pack-{index}"
                                value={pack.id}
                                checked={chosen === pack.id}
                                disabled={!pack.isValid || !canChoose}
                                aria-describedby="pack-{index}-details"
                                onchange={() => choose(pack.id)}
                            />
                            <label for="pack-{index}">
                                <span class="title">{pack.title ?? fr.gm.packs.untitled}</span>
                                <span class="folder">
                                    {fill(fr.gm.packs.folder, { folder: pack.id })}
                                </span>
                            </label>
                        </div>
                        <div id="pack-{index}-details" class="details">
                            <p class="state">{stateOf(pack)}</p>
                            {#if pack.isValid}
                                <button
                                    type="button"
                                    disabled={!interactive}
                                    onclick={() => (previewing = pack.id)}
                                >
                                    {fr.gm.preview.action}
                                </button>
                                <ol class="rounds">
                                    {#each pack.rounds as round, number (number)}
                                        <li>
                                            {fill(fr.gm.packs.round, {
                                                title: round.title,
                                                mode: describeMode(round.mode),
                                            })}
                                        </li>
                                    {/each}
                                </ol>
                            {:else if pack.roundCount !== null}
                                <p class="hint">
                                    {countText(fr.gm.packs.roundCount, pack.roundCount)}
                                </p>
                            {/if}
                        </div>
                        {#if !pack.isValid}
                            <details>
                                <summary>{fr.gm.packs.showProblems}</summary>
                                <ul class="problems">
                                    {#each pack.problems as problem, number (number)}
                                        <li>
                                            <span class="message">{describeProblem(problem)}</span>
                                            <span class="location">
                                                {fill(fr.gm.packs.location, {
                                                    file: problem.file,
                                                    path: problem.path,
                                                })}
                                            </span>
                                        </li>
                                    {/each}
                                </ul>
                            </details>
                        {/if}
                    </li>
                {/each}
            </ul>
        {/if}
        {#if selectFailed}
            <p class="problem" role="alert">{fr.gm.packs.selectFailed}</p>
        {/if}
        <div class="reload">
            <button
                type="button"
                disabled={!interactive || reloading}
                aria-describedby="packs-reload-hint"
                onclick={reload}
            >
                {reloading ? fr.gm.packs.reloading : fr.gm.packs.reload}
            </button>
            <p id="packs-reload-hint" class="hint">{fr.gm.packs.reloadHint}</p>
            {#if reloadFailed}
                <p class="problem" role="alert">{fr.gm.packs.reloadFailed}</p>
            {/if}
        </div>
    {/if}
</section>

{#if previewing !== null}
    <ConfirmDialog
        title={fr.gm.preview.confirmTitle}
        message={fr.gm.preview.confirmMessage}
        confirmLabel={fr.gm.preview.confirm}
        cancelLabel={fr.gm.preview.cancel}
        confirmDisabled={!interactive}
        onconfirm={preview}
        oncancel={() => (previewing = null)}
    />
{/if}

<style>
    .packs,
    .reload {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
    }

    h2,
    p,
    ul,
    ol {
        margin: 0;
    }

    h2 {
        font-size: 1.25rem;
    }

    .hint,
    .folder,
    .state,
    .location {
        color: var(--color-text-muted);
        font-size: 0.875rem;
    }

    .directory,
    .location {
        overflow-wrap: anywhere;
    }

    .none,
    .played {
        font-weight: 700;
    }

    ul,
    .problems {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
        padding: 0;
        list-style: none;
    }

    li {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
    }

    ul[aria-label] > li {
        padding: var(--space-s) var(--space-m);
        border: 2px solid transparent;
        border-radius: var(--radius);
        background: var(--color-surface);
    }

    ul[aria-label] > li.chosen {
        border-color: var(--color-accent);
    }

    .choice {
        display: flex;
        align-items: center;
        gap: var(--space-m);
        min-height: var(--touch-target-min);
    }

    input {
        flex: none;
        width: 1.5rem;
        height: 1.5rem;
        margin: 0;
        accent-color: var(--color-accent);
        touch-action: manipulation;
    }

    label {
        display: flex;
        flex: 1;
        flex-direction: column;
        min-width: 0;
        cursor: pointer;
        touch-action: manipulation;
    }

    .invalid label {
        cursor: default;
    }

    .title {
        font-size: 1.125rem;
        font-weight: 700;
        overflow-wrap: anywhere;
    }

    .invalid .title {
        opacity: 0.7;
    }

    .details {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
        /* Aligned with the label, past the radio button. */
        padding-left: calc(1.5rem + var(--space-m));
    }

    .rounds {
        padding-left: 1.25rem;
        overflow-wrap: anywhere;
    }

    .invalid .state {
        color: var(--color-accent);
        font-weight: 700;
    }

    summary {
        display: flex;
        align-items: center;
        min-height: var(--touch-target-min);
        color: var(--color-accent);
        font-weight: 700;
        cursor: pointer;
        touch-action: manipulation;
    }

    /* The marker is drawn below, the same on every browser. */
    summary {
        list-style: none;
    }

    summary::-webkit-details-marker {
        display: none;
    }

    summary::before {
        content: '▸';
        margin-right: 0.5em;
    }

    details[open] summary::before {
        content: '▾';
    }

    .problems li {
        gap: 0.125rem;
        padding-left: var(--space-s);
        border-left: 3px solid var(--color-accent);
    }

    .message {
        overflow-wrap: anywhere;
    }

    button {
        align-self: flex-start;
        min-width: var(--touch-target-min);
        min-height: var(--touch-target-min);
        padding: 0 var(--space-m);
        border: 2px solid var(--color-accent);
        border-radius: var(--radius);
        background: transparent;
        color: var(--color-accent);
        font: inherit;
        font-weight: 700;
        cursor: pointer;
        touch-action: manipulation;
    }

    button:disabled {
        opacity: 0.4;
        cursor: default;
    }

    .problem {
        color: var(--color-accent);
        font-weight: 700;
    }
</style>
