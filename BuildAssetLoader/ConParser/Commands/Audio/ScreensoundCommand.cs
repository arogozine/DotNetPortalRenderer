namespace BuildAssetLoader.Con
{
    // screensound <sound#> — unconditionally plays a session-wide sound (e.g. from menus).
    public sealed record ScreensoundCommand(string Sound) : Command(CommandList.screensound);
}

