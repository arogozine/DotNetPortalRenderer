// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Creates a savegame in the given slot (0-9). Unlike <c>savenn</c>, always overwrites the save's
    /// name with an auto-generated "Auto"+date/time name.</summary>
    [Description("save")]
    public sealed record SaveCommand(
        string SlotNumber) : BaseSaveCommand(CommandList.Save, SlotNumber);
}
