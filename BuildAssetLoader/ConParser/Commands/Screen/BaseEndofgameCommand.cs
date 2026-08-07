namespace BuildAssetLoader.Con
{
    // endofgame/endoflevel <number> — triggers end of episode after <number> 1/15-second time units (default 52).
    public record BaseEndofgameCommand(CommandList Start, int Number) : Command(Start);
}

