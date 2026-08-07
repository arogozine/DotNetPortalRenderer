// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Sets the current actor's health (strength) to <c>Number</c>, commonly a defined label such as
    /// MYENEMY_NORMAL_STRENGTH or TOUGH.</summary>
    [Description("strength")]
    public sealed record StrengthCommand(string Number) : Command(CommandList.Strength);
}
