// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Jump (deprecated) =====

    /// <summary>Stores the address of this point in the code into a gamevar, so that <c>jump</c> can later
    /// transfer control back here.</summary>
    [Description("getcurraddress")]
    public sealed record GetCurrAddressCommand(
        string Addr) : Command(CommandList.GetCurrAddress);
}
