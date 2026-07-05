using System.Diagnostics;

namespace SoftwareRendererModels;

[DebuggerDisplay("ResourceType = {ResourceType}")]
public sealed record FixedGameState(Map Map, GameResourceType ResourceType) : IFixedState;
