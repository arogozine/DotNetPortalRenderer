// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Player - Commands =====

    /// <summary>Adds ammo for a weapon to the nearest player without giving the weapon itself. If the player
    /// already has full ammo for that weapon, execution of subsequent code halts similarly to <c>return</c>.</summary>
    [Description("addammo")]
    public sealed record AddAmmoCommand(
        string Weapon,
        string Amount) : Command(CommandList.AddAmmo);
}
