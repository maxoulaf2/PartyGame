using PartyGame.Engine.Inputs;
using PartyGame.Engine.Lobby;
using PartyGame.Engine.Modes;
using PartyGame.Engine.Rounds;
using PartyGame.Engine.Scores;

namespace PartyGame.Engine;

/// <summary>
/// The rules of the game, as a pure function: no I/O, no clock, no randomness of its own.
/// The game loop calls it for each input, one at a time.
/// </summary>
/// <param name="modes">The game modes that play the rounds of the packs.</param>
public sealed class GameEngine(GameModes modes) : IGameEngine
{
    /// <inheritdoc />
    public Transition Handle(GameState state, GameInput input, GameContext context)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        if (state.Phase == GamePhase.ResumePending && input is not (ResumeSavedGame or DiscardSavedGame or SavedGameMediaChecked))
        {
            // Nothing happens to a game that may be replaced as a whole: neither registration, nor any choice.
            return Transition.Rejected(state, RejectionReason.GamePending);
        }

        if (state.PausedAt is not null && input is NextRound or StartRound or SkipRound or PlayerRoundInput or GameMasterRoundInput or TimerElapsed)
        {
            // Nothing moves in a round: the timers were cancelled, but one may have elapsed just before the pause.
            return Transition.Rejected(state, RejectionReason.GamePaused);
        }

        return input switch
        {
            ResumeSavedGame resume => SavedGameChoice.Resume(state, resume),
            GameResumed resumed => RoundFlow.Resume(state, resumed, modes, context),
            DiscardSavedGame discard => SavedGameChoice.Discard(state, discard),
            SavedGameMediaChecked check => SavedGameChoice.MediaChecked(state, check),
            JoinGame join => Registration.Join(state, join),
            PlayerConnectionLost lost => Presence.ConnectionLost(state, lost),
            PlayerConnectionRestored restored => Presence.ConnectionRestored(state, restored),
            RenamePlayer rename => Renaming.Rename(state, rename),
            AdjustScore adjust => ScoreAdjustment.Adjust(state, adjust),
            StartGame start => Launch.Start(state, start, modes, context),
            PauseGame pause => GamePause.Handle(state, pause, modes, context),
            ReturnToLobby back => LobbyReturn.Return(state, back, context),
            ChooseAdvertisedAddress choice => AddressChoice.Choose(state, choice),
            ShowJoinCode show => JoinCodeVisibility.Show(state, show),
            SelectPack select => PackChoice.Select(state, select),
            StartPreview preview => PackPreviewing.Start(state, preview, modes, context),
            ShowPreviewStep show => PackPreviewing.Show(state, show, modes, context),
            StopPreview stop => PackPreviewing.Stop(state),
            PacksLoaded loaded => PackChoice.Load(state, loaded),
            NextRound next => RoundFlow.Next(state, next, context),
            StartRound start => RoundFlow.Start(state, start, modes, context),
            SkipRound skip => RoundFlow.Skip(state, skip, context),
            PlayerRoundInput player => RoundFlow.HandlePlayerIntent(state, player, modes, context),
            GameMasterRoundInput gameMaster => RoundFlow.HandleGameMasterIntent(state, gameMaster, modes, context),

            // Only rounds schedule timers so far.
            TimerElapsed timer => RoundFlow.HandleTimer(state, timer, modes, context),

            _ => throw new NotSupportedException($"Input {input.GetType().Name} is not handled by the engine."),
        };
    }
}
