namespace BuildAssetLoader.Con
{
    // soundonce/soundoncevar <sound number> — like sound, but won't restart while an instance is still playing.
    public sealed record SoundonceCommand(string SoundNumber) : BaseSoundCommand(CommandList.soundonce, SoundNumber);
}

