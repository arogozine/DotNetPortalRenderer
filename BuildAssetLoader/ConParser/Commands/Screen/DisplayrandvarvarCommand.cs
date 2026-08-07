// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Like <c>displayrandvar</c>, but <c>MaxValue</c> is itself a gamevar rather than a constant or
    /// defined label.</summary>
    [Description("displayrandvarvar")]
    public sealed record DisplayRandVarVarCommand(
        string Gamevar,
        string MaxValue) : Command(CommandList.DisplayRandVarVar);
}
