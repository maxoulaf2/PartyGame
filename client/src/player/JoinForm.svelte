<script lang="ts">
    import { untrack } from 'svelte';
    import type { JoinOutcome, PlayerSession } from '../shared/connection/playerSession.svelte';
    import Confetti from '../shared/components/Confetti.svelte';
    import Logo from '../shared/components/Logo.svelte';
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
    <Confetti />
    <Logo />
    <div class="card">
        {#if withCode}
            <RecoverForm {session} {interactive} onback={() => (withCode = false)} />
        {:else}
            {@render nicknameForm()}
        {/if}
    </div>
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
        position: relative;
        isolation: isolate;
        display: flex;
        flex-direction: column;
        justify-content: space-between;
        gap: 32px;
        max-width: 30rem;
        min-height: 100vh;
        min-height: 100dvh;
        margin: 0 auto;
        padding: 72px 24px 40px;
        overflow: hidden;
    }

    main > :global(h1) {
        margin-top: 40px;
    }

    .card {
        display: flex;
        flex-direction: column;
        gap: 12px;
        padding: 24px;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: 28px;
        background: var(--color-surface);
        box-shadow: 0 8px 0 var(--color-ink);
        color: var(--color-on-surface);
    }

    /* The two forms of the card, this one and RecoverForm, look alike. */
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

    input {
        min-height: 58px;
        padding: 0 16px;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: 16px;
        background: var(--color-white);
        color: var(--color-ink);
        font: inherit;
        /* At least 16 px: Safari on iOS zooms into a smaller field when it gets the focus. */
        font-size: 22px;
        font-weight: 700;
    }

    input:disabled {
        opacity: 0.6;
    }

    input:focus-visible {
        outline: var(--sticker-line) solid var(--color-bg);
        outline-offset: 2px;
    }

    .problem {
        margin: 0;
        padding: 6px 12px;
        border: 2px solid var(--color-ink);
        border-radius: 12px;
        background: var(--color-pink);
        font-weight: 700;
    }

    button {
        min-height: var(--touch-target-min);
        font: inherit;
        cursor: pointer;
    }

    button[type='submit'] {
        min-height: 60px;
        margin-top: 8px;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: 18px;
        background: var(--color-accent);
        box-shadow: 0 6px 0 var(--color-ink);
        color: var(--color-ink);
        font-size: 22px;
        font-weight: 800;
    }

    button[type='submit']:active:enabled {
        transform: translateY(4px);
        box-shadow: 0 2px 0 var(--color-ink);
    }

    button.secondary {
        border: none;
        background: transparent;
        color: var(--color-on-surface-muted);
        font-weight: 700;
        text-decoration: underline;
        touch-action: manipulation;
    }

    button:disabled {
        opacity: 0.4;
        cursor: default;
    }
</style>
