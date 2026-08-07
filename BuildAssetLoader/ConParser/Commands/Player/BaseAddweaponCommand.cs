namespace BuildAssetLoader.Con
{
    // addweapon/addweaponvar <weapon> <amount> — gives the weapon with ammo to the nearest player.
    public record BaseAddweaponCommand(CommandList Start, string Weapon, string Amount) : Command(Start);
}

