// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Copies the values of the first N entries of gamearray <c>Gamearray</c> into the given
    /// <c>Gamevars</c> (up to 32). Commonly used to unpack a gamearray-backed struct into individually named
    /// variables, e.g. before calling a command that only accepts gamevars. See also <c>setarraysequence</c>.</summary>
    [Description("getarraysequence")]
    public sealed record GetArraySequenceCommand(
        string Gamearray,
        string[] Gamevars) : Command(CommandList.GetArraySequence);
}
