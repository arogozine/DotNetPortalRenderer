// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Sets <c>Gamevar</c> to the sine of the angle held by <c>Angle</c>, scaled so that a hypotenuse of
    /// 1.0 is represented as 16384 (the engine has no fractional values). See also <c>cos</c>.</summary>
    [Description("sin")]
    public sealed record SinCommand(
        string Gamevar,
        string Angle) : Command(CommandList.Sin);
}
