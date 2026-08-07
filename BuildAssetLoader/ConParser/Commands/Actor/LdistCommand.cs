// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Calculates the 2D (x/y only) distance between sprites <c>Sprite1</c> and <c>Sprite2</c>, storing the
    /// result in <c>Gamevar</c>. Compare with <c>dist</c>, which also factors in z.</summary>
    [Description("ldist")]
    public sealed record LDistCommand(
        string Gamevar,
        string Sprite1,
        string Sprite2) : Command(CommandList.LDist);
}
