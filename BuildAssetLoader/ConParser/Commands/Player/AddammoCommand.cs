namespace BuildAssetLoader.Con
{
    // ===== Player - Commands =====

    // addammo <weapon> <amount> — gives ammo without giving the weapon itself. <amount> is frequently a define.
    public sealed record AddammoCommand(string Weapon, string Amount) : Command(CommandList.addammo);
}

