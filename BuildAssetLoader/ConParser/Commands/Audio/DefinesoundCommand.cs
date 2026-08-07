namespace BuildAssetLoader.Con
{
    // ===== Sounds =====

    // definesound <value> <filename> <pitch_lower> <pitch_upper> <priority> <type> <distance> [volume]
    public sealed record DefinesoundCommand(
        string Value, string Filename, int PitchLower, int PitchUpper, int Priority, int Type, int Distance, int? Volume)
        : Command(CommandList.definesound);
}

