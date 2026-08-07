namespace BuildAssetLoader.Con
{
    // cstat <number> — sets the sprite's cstat bitfield (often a CSTAT_SPRITE_* define).
    public sealed record CstatCommand(string Value) : Command(CommandList.cstat);
}

