using PartyGame.Engine.Inputs;

namespace PartyGame.Server.Tests.Games;

/// <summary>
/// An input the real engine does not know, interpreted by <see cref="ScriptedEngine"/>.
/// </summary>
internal sealed record TestInput(int Value) : GameInput;
