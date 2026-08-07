namespace BuildAssetLoader.Con
{
    // ===== Screen Manipulation =====

    // palfrom <intensity> <red> <green> <blue> — flashes the screen a color; red/green/blue default to 0 if omitted.
    public sealed record PalfromCommand(int Intensity, int? Red, int? Green, int? Blue) : Command(CommandList.palfrom);
}

