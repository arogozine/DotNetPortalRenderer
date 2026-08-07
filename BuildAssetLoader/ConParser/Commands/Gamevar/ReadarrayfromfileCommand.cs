// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Loads the file named by quote <c>QuoteNumber</c> into the gamearray <c>ArrayName</c>, 4 bytes per
    /// element, implicitly resizing the array to fit the file's contents. Expects a file previously written with
    /// <c>writearraytofile</c>.</summary>
    [Description("readarrayfromfile")]
    public sealed record ReadArrayFromFileCommand(
        string ArrayName,
        int QuoteNumber) : Command(CommandList.ReadArrayFromFile);
}
