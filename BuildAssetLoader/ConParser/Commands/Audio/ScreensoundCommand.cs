// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Unconditionally plays a session-wide sound, e.g. for menu sound effects.</summary>
    [Description("screensound")]
    public sealed record ScreenSoundCommand(
        string Sound) : Command(CommandList.ScreenSound);
}
