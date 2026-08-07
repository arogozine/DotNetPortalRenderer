namespace BuildAssetLoader.Con
{
    // ===== Audio/Cutscene If =====

    // ifsound <sound> — sound is a sound label define.
    public sealed record IfsoundCommand(string Sound) : ConditionalStructure(CommandList.ifsound);
}

