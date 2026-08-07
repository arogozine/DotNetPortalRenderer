namespace BuildAssetLoader.Con
{
    // respawnhitag — activates respawn sprites whose lotag matches the current actor's hitag.
    public sealed record RespawnhitagCommand() : Command(CommandList.respawnhitag);
}

