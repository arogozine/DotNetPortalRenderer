// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Actors - Structures =====

    /// <summary>Changes the current actor into an actor of tile type <c>Name</c>; equivalent to setting the
    /// sprite's picnum via <c>setactor</c>.</summary>
    [Description("cactor")]
    public sealed record CActorCommand(string Name) : Command(CommandList.CActor);
}
