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
    /* As the nickname form of JoinForm, in the same card. */
    h1 {
        margin: 0;
        font-size: 30px;
        font-weight: 800;
        line-height: 1.1;
        letter-spacing: -0.02em;
    }

    form {
        display: flex;
        flex-direction: column;
        gap: 12px;
    }

    label {
        color: var(--color-on-surface-muted);
        font-size: 15px;
        font-weight: 700;
    }

    input,
    button {
        min-height: var(--touch-target-min);
        font: inherit;
        touch-action: manipulation;
    }

    input {
        min-height: 58px;
        padding: 0 16px;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: 16px;
        background: var(--color-white);
        color: var(--color-ink);
        /* At least 16 px: Safari on iOS zooms into a smaller field when it gets the focus. */
        font-size: 22px;
        font-weight: 700;
        letter-spacing: 0.2em;
        text-transform: uppercase;
    }

    input:disabled {
        opacity: 0.6;
    }

    input:focus-visible {
        outline: var(--sticker-line) solid var(--color-bg);
        outline-offset: 2px;
    }

    .problem,
    .hint {
        margin: 0;
    }

    .problem {
        padding: 6px 12px;
        border: 2px solid var(--color-ink);
        border-radius: 12px;
        background: var(--color-pink);
        font-weight: 700;
    }

    .hint {
        color: var(--color-on-surface-muted);
    }

    button {
        margin-top: 8px;
        min-height: 60px;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: 18px;
        background: var(--color-accent);
        box-shadow: 0 6px 0 var(--color-ink);
        color: var(--color-ink);
        font-size: 22px;
        font-weight: 800;
        cursor: pointer;
    }

    button:active:enabled {
        transform: translateY(4px);
        box-shadow: 0 2px 0 var(--color-ink);
    }

    button.secondary {
        min-height: var(--touch-target-min);
        margin-top: 0;
        border: none;
        background: transparent;
        box-shadow: none;
        color: var(--color-on-surface-muted);
        font-size: inherit;
        font-weight: 700;
        text-decoration: underline;
    }

    button:disabled {
        opacity: 0.4;
        cursor: default;
    }
</style>
