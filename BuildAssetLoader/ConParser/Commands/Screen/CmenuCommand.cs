// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Opens a specific menu screen, identified by <c>Value</c> (see <c>current_menu</c> and the
    /// MENU_* defines).</summary>
    [Description("cmenu")]
    public sealed record CMenuCommand(
        string Value) : Command(CommandList.CMenu);
}
