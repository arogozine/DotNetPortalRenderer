// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Saves the full contents of the gamearray <c>ArrayName</c> to the file named by quote
    /// <c>QuoteNumber</c>, 4 bytes per element. The file can later be reloaded with <c>readarrayfromfile</c>.</summary>
    [Description("writearraytofile")]
    public sealed record WriteArrayToFileCommand(
        string ArrayName,
        int QuoteNumber) : Command(CommandList.WriteArrayToFile);
}
