// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Overrides the default DEF file name to load (e.g. duke3d.def).</summary>
    [Description("setdefname")]
    public sealed record SetDefNameCommand(
        string Name) : Command(CommandList.SetDefName);
}
