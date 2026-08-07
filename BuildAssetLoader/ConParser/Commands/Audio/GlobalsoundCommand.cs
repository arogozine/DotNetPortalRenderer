namespace BuildAssetLoader.Con
{
    // globalsound/globalsoundvar <sound> — audible everywhere in the map.
    public sealed record GlobalsoundCommand(string Sound) : BaseSoundCommand(CommandList.globalsound, Sound);
}

