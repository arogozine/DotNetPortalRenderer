namespace BuildAssetLoader.Con
{
    public sealed record SoundoncevarCommand(string SoundNumber) : BaseSoundCommand(CommandList.soundoncevar, SoundNumber);
}

