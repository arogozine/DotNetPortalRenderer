// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Retrieves the ceiling texture (tile number) of the current actor's sector into gamevar RETURN.</summary>
    [Description("gettextureceiling")]
    public sealed record GetTextureCeilingCommand() : Command(CommandList.GetTextureCeiling);
}
