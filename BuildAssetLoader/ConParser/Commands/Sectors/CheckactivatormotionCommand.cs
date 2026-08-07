namespace BuildAssetLoader.Con
{
    // checkactivatormotion <lotag> — sets RETURN to 1 if an activator with the given lotag is in motion.
    public sealed record CheckactivatormotionCommand(string Lotag) : Command(CommandList.checkactivatormotion);
}

