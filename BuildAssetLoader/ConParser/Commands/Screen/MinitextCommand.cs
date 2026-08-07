// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Deprecated: prints a defined quote to the screen using small text. <c>X</c>/<c>Y</c> set the draw
    /// position, <c>Quote</c> is the quote id, and <c>Shade</c>/<c>Pal</c> set shading/palette. Only works during
    /// screen-drawing events.</summary>
    [Description("minitext")]
    public sealed record MiniTextCommand(
        string X,
        string Y,
        int Quote,
        string Shade,
        string Pal) : Command(CommandList.MiniText);
}
