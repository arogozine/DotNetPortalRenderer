// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Renames one of the game's input functions (as listed in the key configuration menu), for
    /// display and lookup purposes. The new name should use underscores instead of spaces.</summary>
    [Description("definegamefuncname")]
    public sealed record DefineGameFuncNameCommand(
        int Function,
        string Name) : Command(CommandList.DefineGameFuncName);
}
