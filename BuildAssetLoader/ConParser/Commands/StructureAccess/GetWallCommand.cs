// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Reads a member of the wall structure for the wall given by <c>Id</c> into <c>Gamevar</c>. Unlike
    /// most other structs, walls have no THISACTOR-implied default, so <c>Id</c> is effectively required.</summary>
    [Description("getwall")]
    public sealed record GetWallCommand(
        string? Id,
        string Member,
        string Gamevar) : Command(CommandList.GetWall);
}
