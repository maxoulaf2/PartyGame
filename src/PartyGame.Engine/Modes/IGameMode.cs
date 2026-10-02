using PartyGame.Contracts;
using PartyGame.Contracts.Packs;
using PartyGame.Engine.Inputs;

namespace PartyGame.Engine.Modes;

/// <summary>
/// A game mode (quiz, blind test…): the state machine that plays the rounds of one activity type of the packs. The engine
/// hands it the rounds of that type and the inputs aimed at them, and asks it the views of each role. Like the engine, a
/// mode is pure: no I/O, no clock, no randomness of its own.
/// </summary>
/// <remarks>
/// A mode is registered explicitly in <c>AddGameModes()</c>, and its descriptor, views and intents are declared on their
/// polymorphic bases in <c>PartyGame.Contracts</c>. Nothing else changes in the engine, the loop or the hub. Derive from
/// <see cref="GameMode{TDescriptor, TState}"/> rather than implementing this interface directly.
/// </remarks>
public interface IGameMode
{
    /// <summary>
    /// The type of the descriptors of the activities this mode plays, derived from <see cref="RoundDescriptor"/>.
    /// </summary>
    Type DescriptorType { get; }

    /// <summary>
    /// Starts a round, when the game starts or when the game master asks for the next round.
    /// </summary>
    /// <param name="descriptor">The activity of the pack, of type <see cref="DescriptorType"/>.</param>
    /// <param name="game">The game as the round starts, for its players.</param>
    /// <param name="context">Current time and random generator.</param>
    /// <returns>The initial state of the round and its effects. A rejection is a bug of the mode.</returns>
    RoundTransition Start(RoundDescriptor descriptor, GameState game, GameContext context);

    /// <summary>
    /// Handles an input aimed at the round in progress: a <see cref="PlayerRoundInput"/>, a
    /// <see cref="GameMasterRoundInput"/>, or the <see cref="TimerElapsed"/> of a timer the round scheduled. The engine
    /// has already checked that it is aimed at this round, and that a player input comes from a registered player.
    /// </summary>
    /// <param name="round">The state of the round, as this mode produced it.</param>
    /// <param name="input">The input to handle.</param>
    /// <param name="game">The current game, for its players.</param>
    /// <param name="context">Current time and random generator.</param>
    RoundTransition Handle(RoundState round, GameInput input, GameState game, GameContext context);

    /// <summary>
    /// What the phone of <paramref name="player"/> shows. The player may have joined during the round: the mode decides
    /// whether they take part.
    /// </summary>
    /// <param name="round">The state of the round, as this mode produced it.</param>
    /// <param name="game">The current game.</param>
    /// <param name="player">A player registered in <paramref name="game"/>.</param>
    PlayerRoundView ProjectForPlayer(RoundState round, GameState game, Player player);

    /// <summary>
    /// What the TV screen shows: public information only.
    /// </summary>
    /// <param name="round">The state of the round, as this mode produced it.</param>
    /// <param name="game">The current game.</param>
    DisplayRoundView ProjectForDisplay(RoundState round, GameState game);

    /// <summary>
    /// What the game master console shows, the only view allowed to hold the answers before their reveal.
    /// </summary>
    /// <param name="round">The state of the round, as this mode produced it.</param>
    /// <param name="game">The current game.</param>
    GameMasterRoundView ProjectForGameMaster(RoundState round, GameState game);
}
