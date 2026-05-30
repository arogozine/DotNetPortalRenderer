namespace SoftwareRendererModels;

public interface IUniqueName
{
    string Name { get; }
}

/// <summary>
/// Immutable / Loaded State
/// </summary>
public interface IFixedState
{

}

/// <summary>
/// Game State. Such as animation frame.
/// </summary>
public interface IGameState
{

}

/// <summary>
/// Render calculations for current frame.
/// </summary>
public interface IRenderState
{

}