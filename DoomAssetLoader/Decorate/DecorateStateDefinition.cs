// AI Assisted
namespace DoomAssetLoader.Decorate
{
    public sealed record DecorateStateDefinition(
        string Sprite,
        string Frames,
        string Duration,
        bool Bright,
        bool CanRaise,
        bool Fast,
        bool Slow,
        bool NoDelay,
        string? Light,
        (int X, int Y)? Offset,
        string? ActionFunction,
        IReadOnlyList<string>? ActionArguments,
        string? RawActionBlock
    ) : DecorateStateEntry;
}
