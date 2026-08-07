namespace BuildAssetLoader.Con
{
    public sealed record SoundvarCommand(string SoundNumber) : BaseSoundCommand(CommandList.soundvar, SoundNumber);
}

