// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Assigns <c>Value</c> to a member of the sector structure (floors, ceilings, and other sector
    /// properties) for the sector given by <c>Id</c>. When <c>Id</c> is omitted it defaults to the current
    /// sprite's sector.</summary>
    [Description("setsector")]
    public sealed record SetSectorCommand(
        string? Id,
        string Member,
        string Value) : Command(CommandList.SetSector);
}
