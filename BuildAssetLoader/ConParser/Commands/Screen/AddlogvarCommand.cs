namespace BuildAssetLoader.Con
{
    // addlogvar <gamevar> — alias-grammar sibling of addlog.
    public sealed record AddlogvarCommand(string Gamevar) : Command(CommandList.addlogvar);
}

