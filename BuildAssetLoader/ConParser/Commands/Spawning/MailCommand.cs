// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Causes the current actor to spawn the given number of envelope pickups.</summary>
    [Description("mail")]
    public sealed record MailCommand(
        string Number) : Command(CommandList.Mail);
}
