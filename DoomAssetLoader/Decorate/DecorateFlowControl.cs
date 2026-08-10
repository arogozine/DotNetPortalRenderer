// AI Assisted
namespace DoomAssetLoader.Decorate
{
    public sealed record DecorateFlowControl(
        DecorateFlowControlKind Kind,
        string? GotoLabel = null,
        string? GotoScope = null,
        int GotoOffset = 0
    ) : DecorateStateEntry;
}
