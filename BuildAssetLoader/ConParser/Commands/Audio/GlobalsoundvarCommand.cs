namespace BuildAssetLoader.Con
{
    public sealed record GlobalsoundvarCommand(string Sound) : BaseSoundCommand(CommandList.globalsoundvar, Sound);
}

