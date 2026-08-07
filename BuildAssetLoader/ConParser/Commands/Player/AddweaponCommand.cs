namespace BuildAssetLoader.Con
{
    public sealed record AddweaponCommand(string Weapon, string Amount) : BaseAddweaponCommand(CommandList.addweapon, Weapon, Amount);
}

