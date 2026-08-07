// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Like <c>save</c>, but if the savegame in the target slot already has a non-empty name, that name
    /// is kept instead of being overwritten.</summary>
    [Description("savenn")]
    public sealed record SaveNnCommand(
        string SlotNumber) : BaseSaveCommand(CommandList.SaveNn, SlotNumber);
}
