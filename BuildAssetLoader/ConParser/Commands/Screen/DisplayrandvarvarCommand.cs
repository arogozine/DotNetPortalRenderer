namespace BuildAssetLoader.Con
{
    // displayrandvarvar <gamevar> <maxvalue_gamevar> — like displayrandvar, but maxvalue is itself a gamevar.
    public sealed record DisplayrandvarvarCommand(string Gamevar, string MaxValue) : Command(CommandList.displayrandvarvar);
}

