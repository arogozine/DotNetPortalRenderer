// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Player If =====

    /// <summary>Works alongside addweapon in cooperative mode: once addweapon has been used on an actor of the
    /// given picnum (Number), this returns false for further calls on the same picnum. Always returns false in
    /// single player mode.</summary>
    [Description("ifgotweaponce")]
    public sealed record IfGotWeaponOnceCommand(
        string Number) : ConditionalStructure(CommandList.IfGotWeaponOnce);
}
