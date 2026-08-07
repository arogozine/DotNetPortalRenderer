// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Projectiles =====

    /// <summary>Sets one property (identified by a PROJ_* function name, e.g. PROJ_WORKSLIKE, PROJ_VEL, PROJ_EXTRA)
    /// of a custom projectile tile to a given value. Used declaratively outside actor/state code to configure a
    /// projectile before it is fired by shoot and its variants.</summary>
    [Description("defineprojectile")]
    public sealed record DefineProjectileCommand(
        string TileNum,
        string Function,
        string Value) : Command(CommandList.DefineProjectile);
}
