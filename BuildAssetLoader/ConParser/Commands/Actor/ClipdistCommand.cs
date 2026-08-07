namespace BuildAssetLoader.Con
{
    // clipdist <number> — sets the actor's clipping sphere radius.
    public sealed record ClipdistCommand(string Number) : Command(CommandList.clipdist);
}

