// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Briefly clears the entire level's visibility (like an EXPLOSION2 flash) and sets the current
    /// sprite's shade to -127.</summary>
    [Description("flash")]
    public sealed record FlashCommand() : Command(CommandList.Flash);
}
