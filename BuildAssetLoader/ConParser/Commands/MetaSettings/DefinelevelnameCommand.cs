// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Registers a level for use in the game. Episode and LevelNum place the map chronologically
    /// (0-63), MapName is the map file's name, ParTime and DesignerTime are MM:SS-formatted completion time
    /// strings (not integers), and LevelName is the name shown in-game.</summary>
    [Description("definelevelname")]
    public sealed record DefineLevelNameCommand(
        int Episode,
        int LevelNum,
        string MapName,
        string ParTime,
        string DesignerTime,
        string LevelName) : Command(CommandList.DefineLevelName);
}
