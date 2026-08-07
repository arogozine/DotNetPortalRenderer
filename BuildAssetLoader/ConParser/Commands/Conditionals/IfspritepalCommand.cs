// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Conditional returning true if the current actor's pal (palette) is equal to Pal.</summary>
    [Description("ifspritepal")]
    public sealed record IfSpritePalCommand(
        string Pal) : ConditionalStructure(CommandList.IfSpritePal);
}
