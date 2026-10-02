<script lang="ts">
    import {
        isCodeComplete,
        type GameMasterSession,
    } from '../shared/connection/gameMasterSession.svelte';
    import { fr } from '../shared/i18n/fr';

    interface Props {
        session: GameMasterSession;
        /** Whether the page is synchronized with the server: the form is disabled otherwise. */
        interactive: boolean;
    }

    let { session, interactive }: Props = $props();

    let code = $state('');
    let sending = $state(false);
    let input: HTMLInputElement | undefined = $state();

    // The form has nothing else to do: ready to type as soon as it shows, and again once a lost
    // connection is back, the disabled field having lost the focus.
    $effect(() => {
        if (interactive) {
            input?.focus();
        }
    });

    const canSubmit = $derived(interactive && !sending && isCodeComplete(code));
    const message = $derived(
        session.problem === 'invalid'
            ? fr.gm.code.invalid
            : session.problem === 'expired'
              ? fr.gm.code.expired
              : null,
    );

    async function submit(event: SubmitEvent) {
        event.preventDefault();
        if (!canSubmit) {
            return;
        }
        sending = true;
        const outcome = await session.submit(code);
        sending = false;
        if (outcome === 'refused') {
            code = '';
            input?.focus();
        }
    }
</script>

<main>
    <h1>{fr.gm.code.title}</h1>
    <p>{fr.gm.code.instructions}</p>
    <form onsubmit={submit} novalidate>
        <label for="gm-code">{fr.gm.code.label}</label>
        <!-- Text with a numeric keyboard rather than type=number: leading zeros and spaces survive. -->
        <input
            id="gm-code"
            bind:this={input}
            bind:value={code}
            type="text"
            inputmode="numeric"
            pattern="[0-9 ]*"
            autocomplete="one-time-code"
            spellcheck="false"
            disabled={!interactive}
            aria-invalid={message !== null}
            aria-describedby={message ? 'gm-code-problem' : undefined}
        />
        {#if message}
            <p id="gm-code-problem" class="problem" role="alert">{message}</p>
        {/if}
        <button type="submit" disabled={!canSubmit}>{fr.gm.code.submit}</button>
    </form>
</main>

<style>
    main {
        display: flex;
        flex-direction: column;
        justify-content: center;
        gap: var(--space-m);
        max-width: 24rem;
        min-height: 100vh;
        min-height: 100dvh;
        margin: 0 auto;
        padding: var(--space-l);
    }

    h1,
    p {
        margin: 0;
    }

    h1 {
        color: var(--color-accent);
        font-size: var(--font-size-title);
    }

    main > p {
        color: var(--color-text-muted);
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
    }

    input {
        padding: 0 var(--space-m);
        border: 2px solid var(--color-text-muted);
        background: var(--color-surface);
        color: var(--color-text);
        font-size: 1.5em;
        letter-spacing: 0.3em;
        text-align: center;
    }

    input:disabled {
        opacity: 0.6;
    }

    input:focus-visible {
        border-color: var(--color-accent);
        outline: none;
    }

    .problem {
        color: var(--color-accent);
        font-weight: 700;
    }

    button {
        margin-top: var(--space-s);
        border: none;
        background: var(--color-accent);
        color: var(--color-bg);
        font-weight: 700;
        cursor: pointer;
    }

    button:disabled {
        opacity: 0.4;
        cursor: default;
    }
</style>
