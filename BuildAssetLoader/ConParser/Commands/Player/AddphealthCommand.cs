namespace BuildAssetLoader.Con
{
    // addphealth <amount> — adds (or subtracts) player health. Commonly a define (e.g. SHARKBITESTRENGTH).
    public sealed record AddphealthCommand(string Amount) : Command(CommandList.addphealth);
}

