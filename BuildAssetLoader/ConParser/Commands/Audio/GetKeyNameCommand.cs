// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Looks up the keybind for gamefunc <c>FuncId</c>, slot <c>Key</c> (0 = first bound key, 1 = alternate
    /// key, 2 = first key or the alternate if the first is undefined), and stores the resulting key name into quote
    /// <c>QuoteId</c>. If no key is bound for the given slot, the quote is left unchanged. Only supports keyboard
    /// keys.</summary>
    [Description("getkeyname")]
    public sealed record GetKeyNameCommand(
        int QuoteId,
        string FuncId,
        string Key) : Command(CommandList.GetKeyName);
}
