<script lang="ts">
    import type { PlayerSession, RecoverOutcome } from '../shared/connection/playerSession.svelte';
    import { fr } from '../shared/i18n/fr';

    interface Props {
        session: PlayerSession;
        /** Whether the page is synchronized with the server: the form is disabled otherwise. */
        interactive: boolean;
        /** Back to the nickname form. */
        onback: () => void;
    }

    let { session, interactive, onback }: Props = $props();

    let code = $state('');
    let sending = $state(false);
    // The refusal of the server, shown while the field still holds the code it refused.
    let refused = $state<{ code: string; outcome: RecoverOutcome } | null>(null);
    let input: HTMLInputElement | undefined = $state();

    const canSubmit = $derived(interactive && !sending && code.trim() !== '');
    const texts = fr.player.recover;
    const message = $derived(
        refused === null || refused.code !== code
            ? null
            : refused.outcome === 'CodeUnknown'
              ? texts.problems.unknown
              : texts.problems.failed,
    );

    $effect(() => {
        if (interactive) {
            input?.focus();
        }
    });

    async function submit(event: SubmitEvent) {
        event.preventDefault();
        if (!canSubmit) {
            return;
        }
        sending = true;
        const sent = code;
        const outcome = await session.recover(sent);
        sending = false;
        // An unreachable server is for the connection indicator to show, not the field.
        refused =
            outcome === 'recognized' || outcome === 'unreachable' ? null : { code: sent, outcome };
        if (refused !== null) {
            input?.focus();
        }
    }
</script>

<h1>{texts.title}</h1>
<form onsubmit={submit} novalidate>
    <label for="reconnection-code">{texts.label}</label>
    <input
        id="reconnection-code"
        bind:this={input}
        bind:value={code}
        type="text"
        autocomplete="one-time-code"
        autocapitalize="characters"
        autocorrect="off"
        spellcheck="false"
        enterkeyhint="go"
        disabled={!interactive}
        aria-invalid={message !== null}
        aria-describedby={message ? 'reconnection-code-problem' : 'reconnection-code-hint'}
    />
    {#if message}
        <p id="reconnection-code-problem" class="problem" role="alert">{message}</p>
    {:else}
        <p id="reconnection-code-hint" class="hint">{texts.hint}</p>
    {/if}
    <button type="submit" disabled={!canSubmit}>{texts.submit}</button>
    <button type="button" class="secondary" onclick={onback}>{texts.back}</button>
</form>

<style>
    h1 {
        margin: 0;
        color: var(--color-accent);
        font-size: var(--font-size-title);
    }

    form {
        display: flex;
        flex-direction: column;
        gap: var(--space-s);
    }

    input,
    button {
        min-height: var(--touch-target-min);
        border-radius: var(--radius);
        font: inherit;
        touch-action: manipulation;
    }

    input {
        padding: 0 var(--space-m);
        border: 2px solid var(--color-text-muted);
        background: var(--color-surface);
        color: var(--color-text);
        /* At least 16 px: Safari on iOS zooms into a smaller field when it gets the focus. */
        font-size: 1.25rem;
        letter-spacing: 0.2em;
        text-transform: uppercase;
    }

    input:disabled {
        opacity: 0.6;
    }

    input:focus-visible {
        border-color: var(--color-accent);
        outline: none;
    }

    .problem,
    .hint {
        margin: 0;
    }

    .problem {
        color: var(--color-accent);
        font-weight: 700;
    }

    .hint {
        color: var(--color-text-muted);
    }

    button {
        margin-top: var(--space-s);
        border: none;
        background: var(--color-accent);
        color: var(--color-bg);
        font-weight: 700;
        cursor: pointer;
    }

    button.secondary {
        border: 2px solid var(--color-text-muted);
        background: transparent;
        color: var(--color-text);
    }

    button:disabled {
        opacity: 0.4;
        cursor: default;
    }
</style>
