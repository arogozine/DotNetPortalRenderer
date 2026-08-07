// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Commands =====

    /// <summary>Begins the current actor falling under gravity, updating its floor/ceiling tracking each tic and
    /// applying impact damage (plus a JIBS6 spawn and THUD sound) on landing. Gravity is reduced or removed entirely
    /// in sectors with a parallaxed sky ceiling/floor.</summary>
    [Description("fall")]
    public sealed record FallCommand() : Command(CommandList.Fall);
}
