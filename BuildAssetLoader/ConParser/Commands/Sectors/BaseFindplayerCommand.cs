namespace BuildAssetLoader.Con
{
    // findplayer/findotherplayer <gamevar> — distance to nearest player into <gamevar>, id into RETURN.
    public record BaseFindplayerCommand(CommandList Start, string Gamevar) : Command(Start);
}

