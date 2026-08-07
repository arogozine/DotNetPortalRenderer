namespace BuildAssetLoader.Con
{
    // ===== Measurements =====

    // dist <gamevar> <sprite1> <sprite2> — 3D distance between two sprites.
    public sealed record DistCommand(string Gamevar, string Sprite1, string Sprite2) : Command(CommandList.dist);
}

