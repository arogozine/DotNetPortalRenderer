// See: https://wiki.eduke32.com/wiki/Category:All_commands

namespace BuildAssetLoader.Con
{
    // ===== Game Saving =====

    /// <summary>Shared base for the <c>save</c>/<c>savenn</c> commands. Creates a savegame in slot 0-9
    /// (<c>SlotNumber</c>); <c>savenn</c> keeps an existing save's name if it already has one. <c>Start</c>
    /// selects which of the two token spellings was used.</summary>
    public record BaseSaveCommand(CommandList Start, string SlotNumber) : Command(Start);
}
