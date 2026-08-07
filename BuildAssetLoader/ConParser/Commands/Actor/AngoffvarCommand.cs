// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Sets the angle offset applied when rendering the current actor's 3D model, from a gamevar.
    /// <c>Value</c> is the gamevar holding the offset to apply.</summary>
    [Description("angoffvar")]
    public sealed record AngOffVarCommand(string Value) : Command(CommandList.AngOffVar);
}
