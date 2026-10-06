<script lang="ts">
    import Confetti from '../shared/components/Confetti.svelte';
    import type { PlayerSnapshot } from '../shared/contracts';
    import { fr } from '../shared/i18n/fr';

    interface Props {
        snapshot: PlayerSnapshot;
    }

    let { snapshot }: Props = $props();
</script>

<main>
    <Confetti />
    <h1>{fr.app.name}</h1>
    <div class="card">
        <p class="heading">{fr.player.registeredAs}</p>
        <!-- Plain text interpolation: Svelte escapes it, so a nickname is never read as HTML. -->
        <p class="nickname">{snapshot.nickname}</p>
    </div>
    <p class="status">{fr.player.waiting}</p>
    <div class="dots" aria-hidden="true">
        <span></span>
        <span></span>
        <span></span>
    </div>
</main>

<style>
    main {
        position: relative;
        isolation: isolate;
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: 40px;
        min-height: 100vh;
        min-height: 100dvh;
        padding: 40px 28px;
        overflow: hidden;
        text-align: center;
    }

    h1,
    p {
        margin: 0;
    }

    h1 {
        font-size: 30px;
        font-weight: 800;
        letter-spacing: -0.02em;
    }

    .card {
        width: 100%;
        max-width: 26rem;
        overflow: hidden;
        border: var(--sticker-line) solid var(--color-ink);
        border-radius: 28px;
        background: var(--color-surface);
        box-shadow: 0 8px 0 var(--color-ink);
        color: var(--color-on-surface);
        transform: rotate(-3deg);
    }

    .heading {
        padding: 14px 16px;
        border-bottom: var(--sticker-line) solid var(--color-ink);
        background: var(--color-pink);
        font-size: 17px;
        font-weight: 800;
    }

    .nickname {
        padding: 32px 16px 38px;
        font-size: 68px;
        font-weight: 800;
        line-height: 1;
        letter-spacing: -0.03em;
        overflow-wrap: anywhere;
    }

    .status {
        font-size: 22px;
        font-weight: 700;
        line-height: 1.3;
        text-wrap: pretty;
    }

    .dots {
        display: flex;
        gap: 10px;
    }

    .dots span {
        width: 16px;
        height: 16px;
        border: 2px solid var(--color-ink);
        border-radius: 50%;
        background: var(--color-accent);
    }

    .dots span:nth-child(2) {
        background: var(--color-blue);
    }

    .dots span:nth-child(3) {
        background: var(--color-green);
    }
</style>
