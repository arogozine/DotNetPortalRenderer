// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Deprecated command. Sets the current actor's angle from <c>Gamevar</c>. Superseded by struct
    /// access (e.g. <c>setactor[].ang</c>).</summary>
    [Description("setactorangle")]
    public sealed record SetActorAngleCommand(
        string Gamevar) : Command(CommandList.SetActorAngle);
}
