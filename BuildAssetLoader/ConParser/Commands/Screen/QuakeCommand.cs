// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Shakes the screen for <c>Count</c> tics (26 tics is one second). Also triggers sector effectors
    /// with lotag 33 (metal-scrap spawners) as if an earthquake occurred.</summary>
    [Description("quake")]
    public sealed record QuakeCommand(
        string Count) : Command(CommandList.Quake);
}
