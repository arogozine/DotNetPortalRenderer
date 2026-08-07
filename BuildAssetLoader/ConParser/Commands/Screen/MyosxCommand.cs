// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Deprecated: like <c>myos</c>, but the tile is drawn at half size.</summary>
    [Description("myosx")]
    public sealed record MyosXCommand(
        string X,
        string Y,
        string TileNum,
        string Shade,
        string Orientation) : Command(CommandList.MyosX);
}
