namespace BuildAssetLoader.Con
{
    // addlog <gamevar> — logs a gamevar/gamearray value to the console and eduke32.log.
    public sealed record AddlogCommand(string Gamevar) : Command(CommandList.addlog);
}

