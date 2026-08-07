// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Sets the angle offset applied when rendering the current actor's 3D model, from a constant or
    /// defined label. <c>Value</c> is the offset to apply.</summary>
    [Description("angoff")]
    public sealed record AngOffCommand(string Value) : Command(CommandList.AngOff);
}
