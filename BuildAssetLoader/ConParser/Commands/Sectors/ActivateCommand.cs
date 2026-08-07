// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Deprecated command from pre-v1.0 prototypes of Duke Nukem 3D; in EDuke32 it behaves the same as
    /// <c>operateactivators</c> but with a fixed player id of 0, triggering ACTIVATOR/ACTIVATORLOCKED sprites
    /// identified by the given lotag.</summary>
    [Description("activate")]
    public sealed record ActivateCommand(string? Lotag) : Command(CommandList.Activate);
}
