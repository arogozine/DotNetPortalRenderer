namespace BuildAssetLoader.Con
{
    public sealed record StopsoundvarCommand(string SoundNumber) : BaseSoundCommand(CommandList.stopsoundvar, SoundNumber);
}

