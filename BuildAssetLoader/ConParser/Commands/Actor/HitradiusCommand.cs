// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Surroundings - Commands =====

    /// <summary>Inflicts radius damage on actors within <c>Radius</c> of the current sprite, from constants or
    /// defined labels. Damage is split into four concentric bands, from <c>Damage1</c> (furthest from center) to
    /// <c>Damage4</c> (closest). Actors damaged this way report picnum RADIUSEXPLOSION to <c>ifwasweapon</c>.</summary>
    [Description("hitradius")]
    public sealed record HitRadiusCommand(
        string Radius,
        string Damage1,
        string Damage2,
        string Damage3,
        string Damage4)
        : Command(CommandList.HitRadius);
}
