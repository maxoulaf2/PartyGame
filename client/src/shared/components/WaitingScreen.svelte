<script lang="ts">
    interface Props {
        title: string;
        message: string;
    }

    let { title, message }: Props = $props();
</script>

<main>
    <h1>{title}</h1>
    <p>{message}</p>
</main>

<style>
    main {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        gap: var(--space-m);
        min-height: 100vh;
        min-height: 100dvh;
        padding: var(--space-l);
        text-align: center;
    }

    h1 {
        margin: 0;
        color: var(--color-accent);
        font-size: var(--font-size-title);
    }

    p {
        margin: 0;
        color: var(--color-text-muted);
    }
</style>
