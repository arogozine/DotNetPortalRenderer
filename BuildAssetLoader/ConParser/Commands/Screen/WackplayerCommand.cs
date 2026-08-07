namespace BuildAssetLoader.Con
{
    // ===== Player Actions =====

    // wackplayer — tilts the screen as if the player was struck; resets vertical mouse aim (semi-obsolete).
    public sealed record WackplayerCommand() : Command(CommandList.wackplayer);
}

