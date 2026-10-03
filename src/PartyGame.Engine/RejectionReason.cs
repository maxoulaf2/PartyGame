namespace PartyGame.Engine;

/// <summary>
/// Why the engine rejected an input. A rejection is a normal case, not an error: the state stays the same.
/// </summary>
public enum RejectionReason
{
    /// <summary>A timer elapsed that the current state no longer waits for.</summary>
    UnexpectedTimer,

    /// <summary>The player identifier or token is already registered, for instance when a registration is replayed.</summary>
    PlayerAlreadyJoined,

    /// <summary>The nickname is empty, too long, or contains control or invisible characters.</summary>
    NicknameInvalid,

    /// <summary>Another player already has this nickname, ignoring case and accents.</summary>
    NicknameTaken,

    /// <summary>The input concerns a player who is not registered.</summary>
    PlayerUnknown,

    /// <summary>The player is already shown as disconnected.</summary>
    PlayerAlreadyDisconnected,

    /// <summary>The player is already shown as connected.</summary>
    PlayerAlreadyConnected,

    /// <summary>The game cannot start with fewer players than the minimum.</summary>
    NotEnoughPlayers,

    /// <summary>
    /// The game is already started: a start, a choice of pack and a reload of the packs are only allowed from the lobby.
    /// </summary>
    GameAlreadyStarted,

    /// <summary>The catalog has no pack with this identifier, for instance one removed by a reload meanwhile.</summary>
    PackUnknown,

    /// <summary>The pack has problems: it cannot be chosen until they are fixed and the packs reloaded.</summary>
    PackInvalid,

    /// <summary>The game cannot start before the game master chooses a pack.</summary>
    PackNotSelected,

    /// <summary>The address is not one of the candidates the server offers to the game master.</summary>
    AddressUnknown,

    /// <summary>An activity of the pack has no registered game mode to play it: the game cannot start.</summary>
    GameModeMissing,

    /// <summary>An intent aimed at a round arrived while no round is in progress.</summary>
    NotInRound,

    /// <summary>
    /// The input names another round than the one in progress, or than the one that just finished: it is obsolete, for
    /// instance sent twice or by a second game master console, or aimed at the wrong round.
    /// </summary>
    RoundMismatch,

    /// <summary>The next round is asked for while the game is not between two rounds.</summary>
    NotBetweenRounds,

    /// <summary>The game mode of the round in progress plays no such intent.</summary>
    IntentUnsupported,

    /// <summary>
    /// The intent names another question than the one in progress: it is obsolete, for instance delayed by the network
    /// until the next question, or aimed at the wrong question.
    /// </summary>
    QuestionMismatch,

    /// <summary>
    /// The question in progress is not in the phase the intent acts in: a part of the question shown once every choice is,
    /// an answer while the answers are not open, a reveal before they are locked, a move to the next question before the
    /// reveal, or a question skipped once revealed.
    /// </summary>
    PhaseMismatch,

    /// <summary>
    /// The answer was received after the answers closed, although the loop had not handled their lock yet: what counts is
    /// when the answer arrived.
    /// </summary>
    AnswerTooLate,

    /// <summary>The player already answered the question: only their first answer counts.</summary>
    AlreadyAnswered,

    /// <summary>
    /// The player does not take part in the question, having joined after its answers opened: they play from the next one.
    /// </summary>
    NotParticipating,

    /// <summary>The intent names a choice the question does not have.</summary>
    ChoiceUnknown,

    /// <summary>
    /// The player chose a choice the TV screen does not show yet: their phone unlocks it only once the game master shows it.
    /// </summary>
    ChoiceHidden,

    /// <summary>
    /// The game master shows a part of the question presented that is not the next one to show: already shown, for
    /// instance sent twice or by a second console, or a choice before the question or before the choices that precede it.
    /// </summary>
    PresentationStepMismatch,

    /// <summary>
    /// The intent of a player is numbered up to the last one accepted from them: already handled, it is sent again by a
    /// phone that lost its connection before knowing it.
    /// </summary>
    IntentAlreadyHandled,
}
