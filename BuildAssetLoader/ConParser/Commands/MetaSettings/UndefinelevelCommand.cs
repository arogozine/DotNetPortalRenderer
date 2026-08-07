// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Removes a Level entry from Volume, along with its par-time and designer-time definitions.</summary>
    [Description("undefinelevel")]
    public sealed record UndefineLevelCommand(
        int Volume,
        int Level) : Command(CommandList.UndefineLevel);
}
