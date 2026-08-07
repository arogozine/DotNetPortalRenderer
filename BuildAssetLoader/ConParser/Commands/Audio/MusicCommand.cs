// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Music =====

    /// <summary>Declaratively assigns a sequence of music files as background music for the levels of one episode.
    /// Placed outside actor/event code. <c>Volume</c> is the episode number (counting starts at 1; volume 0 is a
    /// special list of up to 8 files used outside normal level music, e.g. intro/briefing/loading). <c>Levels</c>
    /// are paths to music files (MIDI, Ogg Vorbis or FLAC), one per level in order; not all levels need an
    /// entry.</summary>
    [Description("music")]
    public sealed record MusicCommand(
        int Volume,
        string[] Levels) : Command(CommandList.Music);
}
