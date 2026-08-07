// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Gamevar-driven variant of <c>starttrack</c>: changes the currently playing background music to
    /// <c>Track</c>, taking its track number from a gamevar rather than a constant.</summary>
    [Description("starttrackvar")]
    public sealed record StartTrackVarCommand(
        string Track) : BaseStarttrackCommand(CommandList.StartTrackVar, Track);
}
