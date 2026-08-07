namespace BuildAssetLoader.Con
{
    // resetplayer — reloads the map (single player) and clears the player's inventory.
    public sealed record ResetplayerCommand() : Command(CommandList.resetplayer);
}

