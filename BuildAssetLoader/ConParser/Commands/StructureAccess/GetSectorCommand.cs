// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Reads a member of the sector structure (floors, ceilings, and other sector properties) for the
    /// sector given by <c>Id</c> into <c>Gamevar</c>. When <c>Id</c> is omitted it defaults to the current
    /// sprite's sector.</summary>
    [Description("getsector")]
    public sealed record GetSectorCommand(
        string? Id,
        string Member,
        string Gamevar) : Command(CommandList.GetSector);
}
