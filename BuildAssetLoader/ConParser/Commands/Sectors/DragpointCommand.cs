namespace BuildAssetLoader.Con
{
    // ===== Sectors - Manipulation =====

    // dragpoint <wallnum> <x> <y> — moves a wall vertex, like the map editor.
    public sealed record DragpointCommand(string Wallnum, string X, string Y) : Command(CommandList.dragpoint);
}

