// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Changes the current actor's palette reference number to <c>Number</c>. See <c>getlastpal</c> to
    /// restore the previous palette later.</summary>
    [Description("spritepal")]
    public sealed record SpritePalCommand(string Number) : Command(CommandList.SpritePal);
}
