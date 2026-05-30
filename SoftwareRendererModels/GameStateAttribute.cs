namespace SoftwareRendererModels;

[AttributeUsage(AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
public class GameStateAttribute : Attribute, IGameState;
