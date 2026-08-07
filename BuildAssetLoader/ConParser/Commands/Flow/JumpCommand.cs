// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Transfers control to the code address previously stored in a gamevar by <c>getcurraddress</c>.
    /// The address must have been captured earlier in program order, so this can only jump backward (e.g. to
    /// build a countdown loop), never forward.</summary>
    [Description("jump")]
    public sealed record JumpCommand(
        string Address) : Command(CommandList.Jump);
}
