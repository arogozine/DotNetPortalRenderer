// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Sets <c>ReturnVar</c> to the id of the sector that wall <c>WallId</c> belongs to.</summary>
    [Description("sectorofwall")]
    public sealed record SectorOfWallCommand(
        string ReturnVar,
        string WallId) : Command(CommandList.SectorOfWall);
}
