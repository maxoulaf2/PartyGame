<script lang="ts">
    import { untrack } from 'svelte';
    import type { JoinOutcome, PlayerSession } from '../shared/connection/playerSession.svelte';
    import { fr } from '../shared/i18n/fr';
    import { checkNickname } from '../shared/nickname';
    import RecoverForm from './RecoverForm.svelte';

    interface Props {
        session: PlayerSession;
        /** Whether the page is synchronized with the server: the form is disabled otherwise. */
        interactive: boolean;
    }

    let { session, interactive }: Props = $props();

    // Filled once with the nickname this phone last joined with, if any, then the player's to edit.
    let nickname = $state(untrack(() => session.rememberedNickname));
    let sending = $state(false);
    // The refusal of the server, shown while the field still holds the nickname it refused.
    let refused = $state<{ nickname: string; outcome: JoinOutcome } | null>(null);
    let input: HTMLInputElement | undefined = $state();
    // Whether the player chose to join again with the code the game master reads them.
    let withCode = $state(false);

    const problem = $derived(checkNickname(nickname));
    const canSubmit = $derived(interactive && !sending && problem === null);

    const problems = fr.player.join.problems;
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
        const outcome = await session.join(sent);
        sending = false;
        // An unreachable server is for the connection indicator to show, not the field.
        refused =
            outcome === 'joined' || outcome === 'unreachable' ? null : { nickname: sent, outcome };
        if (refused !== null) {
            input?.focus();
        }
    }
</script>

<main>
    {#if withCode}
        <RecoverForm {session} {interactive} onback={() => (withCode = false)} />
    {:else}
        {@render nicknameForm()}
    {/if}
</main>

{#snippet nicknameForm()}
    <h1>{fr.player.join.title}</h1>
    <form onsubmit={submit} novalidate>
        <label for="nickname">{fr.player.join.label}</label>
        <input
            id="nickname"
            bind:this={input}
            bind:value={nickname}
            type="text"
            autocomplete="nickname"
            autocapitalize="words"
            spellcheck="false"
            enterkeyhint="go"
            disabled={!interactive}
            aria-invalid={message !== null}
            aria-describedby={message ? 'nickname-problem' : undefined}
        />
        {#if message}
            <p id="nickname-problem" class="problem" role="alert">{message}</p>
        {/if}
        <button type="submit" disabled={!canSubmit}>{fr.player.join.submit}</button>
    </form>
    <button type="button" class="secondary" onclick={() => (withCode = true)}>
        {fr.player.recover.action}
    </button>
{/snippet}

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
    }

    input {
        padding: 0 var(--space-m);
        border: 2px solid var(--color-text-muted);
        background: var(--color-surface);
        color: var(--color-text);
        /* At least 16 px: Safari on iOS zooms into a smaller field when it gets the focus. */
        font-size: 1.25rem;
    }

    input:disabled {
        opacity: 0.6;
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
        touch-action: manipulation;
    }

    button:disabled {
        opacity: 0.4;
        cursor: default;
    }
</style>
