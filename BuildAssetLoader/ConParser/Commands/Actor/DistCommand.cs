// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Measurements =====

    /// <summary>Calculates the 3D distance between sprites <c>Sprite1</c> and <c>Sprite2</c>, storing the result in
    /// <c>Gamevar</c>. Compare with <c>ldist</c>, which only considers x/y.</summary>
    [Description("dist")]
    public sealed record DistCommand(
        string Gamevar,
        string Sprite1,
        string Sprite2) : Command(CommandList.Dist);
}
