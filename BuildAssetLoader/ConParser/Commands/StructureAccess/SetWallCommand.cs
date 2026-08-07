// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Assigns <c>Value</c> to a member of the wall structure for the wall given by <c>Id</c>. Unlike
    /// most other structs, walls have no THISACTOR-implied default, so <c>Id</c> is effectively required.</summary>
    [Description("setwall")]
    public sealed record SetWallCommand(
        string? Id,
        string Member,
        string Value) : Command(CommandList.SetWall);
}
