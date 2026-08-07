// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Moves the sprite identified by <c>SpriteId</c> directly to coordinates (<c>X</c>, <c>Y</c>,
    /// <c>Z</c>), automatically updating its sector (no separate <c>changespritesect</c> call needed).</summary>
    [Description("setsprite")]
    public sealed record SetSpriteCommand(string SpriteId, string X, string Y, string Z) : Command(CommandList.SetSprite);
}
