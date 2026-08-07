namespace BuildAssetLoader.Con
{
    // sound/soundvar <sound number> — plays a sound defined by definesound.
    public record BaseSoundCommand(CommandList Start, string SoundNumber) : Command(Start);
}

