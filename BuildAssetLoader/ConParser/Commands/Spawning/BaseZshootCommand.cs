namespace BuildAssetLoader.Con
{
    // zshoot/zshootvar/ezshoot/ezshootvar <zvel> <tile number> — shoot with an explicit z-velocity.
    public record BaseZshootCommand(CommandList Start, string Zvel, string TileNumber) : Command(Start);
}

