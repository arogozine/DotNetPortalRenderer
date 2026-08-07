namespace BuildAssetLoader.Con
{
    // operateactivators <lotag> <player id> — triggers ACTIVATOR/ACTIVATORLOCKED sprites by lotag.
    public sealed record OperateactivatorsCommand(string Lotag, string PlayerId) : Command(CommandList.operateactivators);
}

