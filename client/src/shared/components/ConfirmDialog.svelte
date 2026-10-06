<script lang="ts">
    interface Props {
        title: string;
        message: string;
        confirmLabel: string;
        cancelLabel: string;
        /** Disables the confirmation only, for instance while the connection is lost. */
        confirmDisabled?: boolean;
        onconfirm: () => void;
        /** Called on « cancel », on Escape, and on a tap outside the dialog. */
        oncancel: () => void;
    }

    let {
        title,
        message,
        confirmLabel,
        cancelLabel,
        confirmDisabled = false,
        onconfirm,
        oncancel,
    }: Props = $props();

    let dialog: HTMLDialogElement | undefined = $state();

    // A native modal dialog, never `window.confirm`, which mobile browsers render poorly: it traps
    // the focus, and Escape closes it. Shown as soon as the component is mounted.
    $effect(() => {
        dialog?.showModal();
    });

    function cancel(event: Event) {
        // The parent decides whether the dialog goes away, by unmounting it.
        event.preventDefault();
        oncancel();
    }

    function clickOutside(event: MouseEvent) {
        if (event.target === dialog) {
            oncancel();
        }
    }
</script>

<!-- A tap on the backdrop cancels, like Escape, which fires `cancel`. -->
<dialog
    bind:this={dialog}
    aria-labelledby="confirm-title"
    aria-describedby="confirm-message"
    oncancel={cancel}
    onclick={clickOutside}
>
    <div class="content">
        <h2 id="confirm-title">{title}</h2>
        <p id="confirm-message">{message}</p>
        <div class="actions">
            <button type="button" class="secondary" onclick={oncancel}>{cancelLabel}</button>
            <button type="button" disabled={confirmDisabled} onclick={onconfirm}>
                {confirmLabel}
            </button>
        </div>
    </div>
</dialog>

<style>
    dialog {
        width: min(28rem, calc(100vw - 2 * var(--space-m)));
        padding: 0;
        border: none;
        border-radius: var(--radius);
        background: var(--color-surface);
        color: var(--color-text);
    }

    dialog::backdrop {
        background: var(--color-backdrop);
    }

    .content {
        display: flex;
        flex-direction: column;
        gap: var(--space-m);
        padding: var(--space-l) var(--space-m);
    }

    h2,
    p {
        margin: 0;
    }

    h2 {
        color: var(--color-accent);
    }

    .actions {
        display: flex;
        gap: var(--space-s);
    }

    button {
        flex: 1;
        min-height: var(--touch-target-min);
        padding: 0 var(--space-m);
        border: none;
        border-radius: var(--radius);
        background: var(--color-accent);
        color: var(--color-bg);
        font: inherit;
        font-weight: 700;
        cursor: pointer;
        touch-action: manipulation;
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
