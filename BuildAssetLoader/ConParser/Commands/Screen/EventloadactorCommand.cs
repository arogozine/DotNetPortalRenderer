namespace BuildAssetLoader.Con
{
    // eventloadactor <name/tilenum> { ... } enda — runs when a matching actor is loaded into the map.
    public sealed record EventloadactorCommand(string ActorName) : Structure(CommandList.eventloadactor, CommandList.enda);
}

