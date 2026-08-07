// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Gamevar-driven variant of <c>globalsound</c>: plays a sound (by number/defined name) that can be
    /// heard from anywhere in the map, taking its sound value from a gamevar rather than a constant.</summary>
    [Description("globalsoundvar")]
    public sealed record GlobalSoundVarCommand(
        string Sound) : BaseSoundCommand(CommandList.GlobalSoundVar, Sound);
}
