namespace BuildAssetLoader.Con
{
    // checkavailweapon <playerID> — selects the best available weapon per wchoice.
    public sealed record CheckavailweaponCommand(string PlayerId) : Command(CommandList.checkavailweapon);
}

