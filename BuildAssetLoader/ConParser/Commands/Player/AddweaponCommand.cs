// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Gives the specified weapon with the specified amount of ammo to the nearest player. If the player
    /// already has full ammo for that weapon, execution of subsequent code halts similarly to <c>return</c>
    /// (unless the player doesn't have the weapon itself, in which case execution continues).</summary>
    [Description("addweapon")]
    public sealed record AddWeaponCommand(
        string Weapon,
        string Amount) : BaseAddWeaponCommand(CommandList.AddWeapon, Weapon, Amount);
}
