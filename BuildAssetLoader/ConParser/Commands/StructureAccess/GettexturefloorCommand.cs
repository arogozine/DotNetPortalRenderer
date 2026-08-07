// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Retrieves the floor texture (tile number) of the current actor's sector into gamevar RETURN.</summary>
    [Description("gettexturefloor")]
    public sealed record GetTextureFloorCommand() : Command(CommandList.GetTextureFloor);
}
