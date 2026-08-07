namespace BuildAssetLoader.Con
{
    public sealed record AddweaponvarCommand(string Weapon, string Amount) : BaseAddweaponCommand(CommandList.addweaponvar, Weapon, Amount);
}

