// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Assigns <c>Value</c> to a member of an individual in-flight projectile's structure for the sprite
    /// given by <c>Id</c>. When <c>Id</c> is omitted it defaults to THISACTOR, the current sprite.</summary>
    [Description("setthisprojectile")]
    public sealed record SetThisProjectileCommand(
        string? Id,
        string Member,
        string Value) : Command(CommandList.SetThisProjectile);
}
