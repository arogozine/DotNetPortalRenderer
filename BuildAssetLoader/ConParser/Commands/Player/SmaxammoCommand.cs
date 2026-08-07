namespace BuildAssetLoader.Con
{
    // smaxammo <WeaponID> <maxamount> — sets the global max ammo for a weapon.
    public sealed record SmaxammoCommand(string WeaponId, string MaxAmount) : Command(CommandList.smaxammo);
}

