// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Stops every sound currently playing, similar to what happens when escaping to the menu.</summary>
    [Description("stopallsounds")]
    public sealed record StopAllSoundsCommand() : Command(CommandList.StopAllSounds);
}
