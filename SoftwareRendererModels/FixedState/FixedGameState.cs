namespace SoftwareRendererModels;

public sealed record FixedGameState(Map Map, GameResourceType ResourceType) : IFixedState;
