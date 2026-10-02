<script lang="ts">
    import { untrack } from 'svelte';
    import type { GameMasterPlayer } from '../shared/contracts';
    import type {
        GameMasterSession,
        RenameOutcome,
    } from '../shared/connection/gameMasterSession.svelte';
    import { fr } from '../shared/i18n/fr';
    import { checkNickname } from '../shared/nickname';

    interface Props {
        player: GameMasterPlayer;
        session: GameMasterSession;
        /** Whether the console shows the state of the server, not a snapshot older than a disconnection. */
        fresh: boolean;
        /** Called once the rename is done, or cancelled. */
        onclose: () => void;
    }

    let { player, session, fresh, onclose }: Props = $props();

    // Filled once with the current nickname, then the game master's to edit: a snapshot arriving
    // meanwhile, or a lost connection, never wipes what was typed.
    let nickname = $state(untrack(() => player.nickname));
    let sending = $state(false);
    // The refusal of the server, shown while the field still holds the nickname it refused.
    let refused = $state<{ nickname: string; outcome: RenameOutcome } | null>(null);
    let input: HTMLInputElement | undefined = $state();

    $effect(() => {
        input?.select();
    });

    const problem = $derived(checkNickname(nickname));
    const canSubmit = $derived(session.connected && fresh && !sending && problem === null);
    const fieldId = $derived(`rename-${player.id}`);

    const problems = fr.gm.rename.problems;
    const message = $derived.by(() => {
        if (problem === 'tooLong' || problem === 'invalidCharacters') {
            return problems[problem];
        }
        if (refused === null || refused.nickname !== nickname) {
            return null;
        }
        switch (refused.outcome) {
            case 'NicknameInvalid':
                return problems.invalid;
            case 'NicknameTaken':
                return problems.taken;
            case 'PlayerUnknown':
                return problems.unknown;
            default:
                return problems.failed;
        }
    });

    async function submit(event: SubmitEvent) {
        event.preventDefault();
        if (!canSubmit) {
            return;
        }
        sending = true;
        const sent = nickname;
        const outcome = await session.rename(player.id, sent);
        sending = false;
        if (outcome === 'renamed') {
            onclose();
            return;
        }
        // A lost connection is for the connection indicator to show, not the field.
        refused = outcome === 'unreachable' ? null : { nickname: sent, outcome };
        if (refused !== null) {
            input?.focus();
        }
    }

    function cancel(event: KeyboardEvent) {
        if (event.key === 'Escape') {
            onclose();
        }
    }
</script>

<form onsubmit={submit} novalidate>
    <label for={fieldId}>{fr.gm.rename.label.replace('{nickname}', () => player.nickname)}</label>
    <input
        id={fieldId}
        bind:this={input}
        bind:value={nickname}
        onkeydown={cancel}
        type="text"
        autocomplete="off"
        autocapitalize="words"
        spellcheck="false"
        enterkeyhint="done"
        aria-invalid={message !== null}
        aria-describedby={message ? `${fieldId}-problem` : undefined}
    />
    {#if message}
        <p id="{fieldId}-problem" class="problem" role="alert">{message}</p>
    {/if}
    <div class="actions">
        <button type="button" class="secondary" onclick={onclose}>{fr.gm.rename.cancel}</button>
        <button type="submit" disabled={!canSubmit}>{fr.gm.rename.submit}</button>
    </div>
</form>

<style>
    form {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
    }

    label {
        color: var(--color-text-muted);
        overflow-wrap: anywhere;
    }

    input,
    button {
        min-height: var(--touch-target-min);
        border-radius: var(--radius);
        font: inherit;
    }

    input {
        padding: 0 var(--space-m);
        border: 2px solid var(--color-text-muted);
        background: var(--color-bg);
        color: var(--color-text);
        /* At least 16 px: Safari on iOS zooms into a smaller field when it gets the focus. */
        font-size: 1.25rem;
    }

    input:focus-visible {
        border-color: var(--color-accent);
        outline: none;
    }

    .problem {
        margin: 0;
        color: var(--color-accent);
        font-weight: 700;
    }

    .actions {
        display: flex;
        gap: var(--space-s);
    }

    button {
        flex: 1;
        padding: 0 var(--space-m);
        border: none;
        background: var(--color-accent);
        color: var(--color-bg);
        font-weight: 700;
        cursor: pointer;
    }

    .secondary {
        border: 2px solid var(--color-text-muted);
        background: transparent;
        color: var(--color-text);
    }

    button:disabled {
        opacity: 0.4;
        cursor: default;
    }
</style>
