// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Sets the game's name as seen by the operating system; shown in the window title bar when
    /// running windowed.</summary>
    [Description("setgamename")]
    public sealed record SetGameNameCommand(
        string Name) : Command(CommandList.SetGameName);
}
