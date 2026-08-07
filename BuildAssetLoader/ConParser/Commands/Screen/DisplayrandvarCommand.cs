// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Like <c>displayrand</c>, but limits the generated number to [0, MaxValue] inclusive, where
    /// <c>MaxValue</c> is a constant or defined label.</summary>
    [Description("displayrandvar")]
    public sealed record DisplayRandVarCommand(
        string Gamevar,
        string MaxValue) : Command(CommandList.DisplayRandVar);
}
