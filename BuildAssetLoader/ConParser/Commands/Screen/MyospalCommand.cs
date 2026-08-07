// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Deprecated: like <c>myos</c>, but with an explicit palette (<c>Pal</c>).</summary>
    [Description("myospal")]
    public sealed record MyosPalCommand(
        string X,
        string Y,
        string TileNum,
        string Shade,
        string Orientation,
        string Pal)
        : Command(CommandList.MyosPal);
}
