namespace BuildAssetLoader.Con
{
    // gmaxammo <WeaponID> <return> — gets the global max ammo for a weapon.
    public sealed record GmaxammoCommand(string WeaponId, string ReturnVar) : Command(CommandList.gmaxammo);
}

