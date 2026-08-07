namespace BuildAssetLoader.Con
{
    // starttrack/starttrackvar <track#> — changes the currently playing background music.
    public record BaseStarttrackCommand(CommandList Start, string Track) : Command(Start);
}

