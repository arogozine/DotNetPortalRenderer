namespace BuildAssetLoader.Con
{
    // ===== Projectiles =====

    // defineprojectile <tilenum> <function> <value> — declarative, placed outside actor/state code.
    public sealed record DefineprojectileCommand(string TileNum, string Function, string Value) : Command(CommandList.defineprojectile);
}

