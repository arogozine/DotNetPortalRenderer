namespace BuildAssetLoader.Con
{
    // stopactorsound <sprite ID> <sound#> — stops a sound coming from one specific actor.
    public sealed record StopactorsoundCommand(string SpriteId, string Sound) : Command(CommandList.stopactorsound);
}

