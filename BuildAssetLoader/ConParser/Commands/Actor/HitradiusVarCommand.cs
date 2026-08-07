// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Gamevar-driven form of <c>hitradius</c>: inflicts radius damage on actors within <c>Radius</c> of the
    /// current sprite, using gamevars for its inputs. Damage is split into four concentric bands, from
    /// <c>Damage1</c> (furthest from center) to <c>Damage4</c> (closest).</summary>
    [Description("hitradiusvar")]
    public sealed record HitRadiusVarCommand(
        string Radius,
        string Damage1,
        string Damage2,
        string Damage3,
        string Damage4)
        : Command(CommandList.HitRadiusVar);
}
