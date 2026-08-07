// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Changes the currently playing background music to <c>Track</c>, the track number of the current
    /// episode as defined by the <c>music</c> command (volume 7 accesses intro/briefing/loading music).</summary>
    [Description("starttrack")]
    public sealed record StartTrackCommand(
        string Track) : BaseStarttrackCommand(CommandList.StartTrack, Track);
}
