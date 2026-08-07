// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Causes the player to spawn the currently selected weapon as a pickup when killed.</summary>
    [Description("tossweapon")]
    public sealed record TossWeaponCommand() : Command(CommandList.TossWeapon);
}
