// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Compiles but does nothing, like nullop.</summary>
    [Description("time")]
    public sealed record TimeCommand(
        string Gamevar) : Command(CommandList.Time);
}
