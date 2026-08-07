// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Like <c>rotatesprite</c>, but with an added <c>Alpha</c> parameter (0-255 translucence; a negative
    /// value selects a blend table instead).</summary>
    [Description("rotatespritea")]
    public sealed record RotateSpriteACommand(
        string X,
        string Y,
        string Zoom,
        string Ang,
        string TileNum,
        string Shade,
        string Pal,
        string Orientation,
        string Alpha,
        string X1,
        string Y1,
        string X2,
        string Y2)
        : Command(CommandList.RotateSpriteA);
}
