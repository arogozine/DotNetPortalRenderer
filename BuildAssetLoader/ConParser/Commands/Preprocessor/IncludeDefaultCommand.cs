// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Inserts the contents of the default CON file (EDUKE.CON/GAME.CON/NAM.CON/WW2GI.CON) as if it were
    /// part of the file currently being compiled. Only useful for command-line overrides; otherwise causes infinite
    /// recursion.</summary>
    [Description("includedefault")]
    public sealed record IncludeDefaultCommand() : Command(CommandList.IncludeDefault);
}
