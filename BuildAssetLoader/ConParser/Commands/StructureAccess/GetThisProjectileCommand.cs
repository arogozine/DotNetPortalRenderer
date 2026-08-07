// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Reads a member of an individual in-flight projectile's structure for the sprite given by <c>Id</c>
    /// into <c>Gamevar</c>. When <c>Id</c> is omitted it defaults to THISACTOR, the current sprite.</summary>
    [Description("getthisprojectile")]
    public sealed record GetThisProjectileCommand(
        string? Id,
        string Member,
        string Gamevar) : Command(CommandList.GetThisProjectile);
}
