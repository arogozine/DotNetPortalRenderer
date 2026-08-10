// AI Assisted
namespace DoomAssetLoader.Decorate
{
    public sealed class DecorateActor
    {
        public required string Name { get; init; }

        public string? Parent { get; init; }

        public string? Replaces { get; init; }

        public int? DoomEdNum { get; init; }

        public Dictionary<string, DecorateProperty> Properties { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, bool> Flags { get; } = new(StringComparer.OrdinalIgnoreCase);

        public List<DecorateStateEntry> States { get; } = [];
    }
}
