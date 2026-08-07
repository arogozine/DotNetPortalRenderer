namespace BuildAssetLoader.Con
{
    // setsprite <spriteid> <x> <y> <z> — moves a sprite directly to a position, updating its sector.
    public sealed record SetspriteCommand(string SpriteId, string X, string Y, string Z) : Command(CommandList.setsprite);
}

