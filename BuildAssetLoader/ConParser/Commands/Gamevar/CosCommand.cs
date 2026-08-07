// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Sets <c>Gamevar</c> to the cosine of the angle held by <c>Angle</c>, scaled so that a hypotenuse
    /// of 1.0 is represented as 16384 (the engine has no fractional values). See also <c>sin</c>.</summary>
    [Description("cos")]
    public sealed record CosCommand(
        string Gamevar,
        string Angle) : Command(CommandList.Cos);
}
