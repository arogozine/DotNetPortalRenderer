namespace BuildAssetLoader.Con
{
    // ===== Player If =====

    // ifgotweaponce <number> — number is a weapon id define.
    public sealed record IfgotweaponceCommand(string Number) : ConditionalStructure(CommandList.ifgotweaponce);
}

