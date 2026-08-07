namespace BuildAssetLoader.Con
{
    // ldist <gamevar> <sprite1> <sprite2> — 2D (x/y only) distance between two sprites.
    public sealed record LdistCommand(string Gamevar, string Sprite1, string Sprite2) : Command(CommandList.ldist);
}

