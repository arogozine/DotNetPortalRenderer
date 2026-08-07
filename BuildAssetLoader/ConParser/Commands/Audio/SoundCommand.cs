namespace BuildAssetLoader.Con
{
    public sealed record SoundCommand(string SoundNumber) : BaseSoundCommand(CommandList.sound, SoundNumber);
}

