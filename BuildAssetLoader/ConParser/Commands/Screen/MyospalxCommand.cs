// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Deprecated: like <c>myospal</c>, but the tile is drawn at half size.</summary>
    [Description("myospalx")]
    public sealed record MyosPalXCommand(
        string X,
        string Y,
        string TileNum,
        string Shade,
        string Orientation,
        string Pal)
        : Command(CommandList.MyosPalX);
}
