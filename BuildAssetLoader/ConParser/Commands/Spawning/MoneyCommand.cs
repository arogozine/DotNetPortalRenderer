// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Spawns the given number of dollar bill pickups at the current actor.</summary>
    [Description("money")]
    public sealed record MoneyCommand(
        string Number) : Command(CommandList.Money);
}
