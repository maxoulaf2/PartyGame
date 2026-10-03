<script lang="ts">
    import type { QuizChoiceLetter } from '../../shared/contracts';
    import { choiceColor, choiceShape } from './choiceTheme';

    interface Props {
        letter: QuizChoiceLetter;
        /** The color of the shape: the one of the letter, unless it stands on that very color. */
        color?: string;
    }

    let { letter, color = choiceColor(letter) }: Props = $props();
</script>

<!-- The shape repeats what the letter says, for those who tell the choices apart at a glance:
     screen readers read the letter only. -->
<span class="marker">
    <span
        class="shape"
        aria-hidden="true"
        style:background={color}
        style:clip-path={choiceShape(letter)}
    ></span>
    <span class="letter">{letter}</span>
</span>

<style>
    .marker {
        display: inline-flex;
        flex: none;
        align-items: center;
        gap: 0.3em;
        font-weight: 800;
    }

    .shape {
        display: inline-block;
        width: 1em;
        height: 1em;
    }
</style>
