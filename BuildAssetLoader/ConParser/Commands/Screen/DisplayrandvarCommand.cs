namespace BuildAssetLoader.Con
{
    // displayrandvar <gamevar> <maxvalue_constant> — random number in [0, maxvalue].
    public sealed record DisplayrandvarCommand(string Gamevar, string MaxValue) : Command(CommandList.displayrandvar);
}

