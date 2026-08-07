namespace BuildAssetLoader.Con
{
    // definecheat <cheat number> <text to activate cheat>
    public sealed record DefinecheatCommand(int CheatNumber, string ActivationText) : Command(CommandList.definecheat);
}

