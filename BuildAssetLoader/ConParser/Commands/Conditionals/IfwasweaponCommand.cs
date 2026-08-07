namespace BuildAssetLoader.Con
{
    public sealed record IfwasweaponCommand(string Weapon) : ConditionalStructure(CommandList.ifwasweapon);
}

