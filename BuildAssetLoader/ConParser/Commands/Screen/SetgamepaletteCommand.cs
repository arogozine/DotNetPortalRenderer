// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Deprecated: switches between LOOKUP.DAT base palettes 0-6, identified by <c>PalId</c>.</summary>
    [Description("setgamepalette")]
    public sealed record SetGamePaletteCommand(
        string PalId) : Command(CommandList.SetGamePalette);
}
