// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Sounds =====

    /// <summary>Declaratively defines a sound and its playback properties. <c>Value</c> is the sound's number or a
    /// defined name for it. <c>Filename</c> is the sound file's path. <c>PitchLower</c>/<c>PitchUpper</c> give a
    /// random pitch variation range. <c>Priority</c> (0-255) ranks the sound against other simultaneously playing
    /// sounds. <c>Type</c> is a bitfield of sound flags (looping, ambient, speech, global, etc). <c>Distance</c>
    /// modifies the distance registered between the sound and any listening player (negative increases audible
    /// range, positive reduces it). The optional <c>Volume</c> sets playback volume (255 default, 0 silent, 510
    /// double volume).</summary>
    [Description("definesound")]
    public sealed record DefineSoundCommand(
        string Value,
        string Filename,
        int PitchLower,
        int PitchUpper,
        int Priority,
        int Type,
        int Distance,
        int? Volume)
        : Command(CommandList.DefineSound);
}
