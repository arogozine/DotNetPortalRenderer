namespace BuildAssetLoader.Con
{
    // shoot/shootvar/eshoot/eshootvar <tile number> — fires a projectile from the current actor.
    public record BaseShootCommand(CommandList Start, string TileNumber) : Command(Start);
}

