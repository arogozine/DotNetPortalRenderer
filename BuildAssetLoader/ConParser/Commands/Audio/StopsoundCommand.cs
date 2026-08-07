namespace BuildAssetLoader.Con
{
    // stopsound/stopsoundvar <sound number>
    public sealed record StopsoundCommand(string SoundNumber) : BaseSoundCommand(CommandList.stopsound, SoundNumber);
}

