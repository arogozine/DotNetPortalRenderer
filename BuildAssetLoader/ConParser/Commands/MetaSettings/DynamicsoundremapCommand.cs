// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Lone-keyword primitive that enables the dynamic sound remapping system, letting subsequent
    /// <c>define</c> statements reassign default game sound IDs to new numbers while still playing correctly.
    /// Must appear at the very start of the root CON file, outside any actor/state/event block.</summary>
    [Description("dynamicsoundremap")]
    public sealed record DynamicSoundRemapCommand() : Command(CommandList.DynamicSoundRemap);
}
