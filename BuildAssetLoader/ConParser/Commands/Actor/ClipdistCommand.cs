// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Sets the current actor's clipping-sphere radius (the sprite's clipdist member) to <c>Number</c>;
    /// only relevant for non-flat sprites, which otherwise fall back to the clipdist configured in the map
    /// editor.</summary>
    [Description("clipdist")]
    public sealed record ClipDistCommand(string Number) : Command(CommandList.ClipDist);
}
