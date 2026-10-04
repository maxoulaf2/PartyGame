using System.Collections.Immutable;
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
    /// The type of the state of the rounds this mode plays, derived from <see cref="RoundState"/>: the game state is
    /// persisted with it (<see cref="GameStateJson"/>).
    /// </summary>
    Type StateType { get; }

    /// <summary>
    /// Checks the consistency of an activity of a pack when the pack is loaded: what the simple constraints of the
    /// descriptor (bounds, lengths), checked beforehand, cannot express, such as the rules that tie several of its values
    /// together. A round whose activity passes both never fails because of its content.
    /// </summary>
    /// <param name="descriptor">The activity of the pack, of type <see cref="DescriptorType"/>.</param>
    /// <param name="path">The JSON path of the activity in the descriptor file, such as <c>$.rounds[1]</c>.</param>
    /// <returns>The problems found, located under <paramref name="path"/>, or none when the activity is consistent.</returns>
    ImmutableArray<PackProblem> Validate(RoundDescriptor descriptor, string path);

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

    /// <summary>
    /// The step of the round that shows a media file, such as the number of a quiz question, for the game master to know
    /// what the TV screen could not show. Optional: <see cref="GameMode{TDescriptor, TState}"/> answers
    /// <see langword="null"/> unless the mode tells.
    /// </summary>
    /// <param name="round">The state of the round, as this mode produced it.</param>
    /// <param name="media">The path of a media file of the pack the game plays.</param>
    /// <returns>The step, from 1, or <see langword="null"/> when no step of the round shows the file.</returns>
    int? LocateMedia(RoundState round, MediaPath media);

    /// <summary>
    /// Where the round stands among its steps, such as the question in progress of a quiz, for the game master to know
    /// where a saved game stopped. Optional: <see cref="GameMode{TDescriptor, TState}"/> answers <see langword="null"/>
    /// unless the mode tells.
    /// </summary>
    /// <param name="round">The state of the round, as this mode produced it.</param>
    RoundStep? StepOf(RoundState round);
}
