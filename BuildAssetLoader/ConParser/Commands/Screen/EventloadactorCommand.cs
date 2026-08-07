// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Block, terminated by <c>enda</c>, that runs when an actor matching <c>ActorName</c> (a name or
    /// tile number) is loaded into the map. Despite its name it is not a game event but a distinct block type,
    /// most often used to move settings out of a sprite's hitag/lotag and into a gamevar before they trigger
    /// undesirable hardcoded effects. See also EVENT_LOADACTOR.</summary>
    [Description("eventloadactor")]
    public sealed record EventLoadActorCommand(
        string ActorName) : Structure(CommandList.EventLoadActor, CommandList.Enda);
}
