namespace BuildAssetLoader.Con
{
    // tossweapon — spawns the currently selected weapon on player death.
    public sealed record TossweaponCommand() : Command(CommandList.tossweapon);
}

