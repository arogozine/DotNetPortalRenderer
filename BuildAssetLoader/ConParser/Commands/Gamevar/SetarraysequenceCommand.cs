// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Saves the values of the given <c>Gamevars</c> (up to 32) into the first N entries of gamearray
    /// <c>Gamearray</c>, resizing the array to match the number of gamevars provided. See also
    /// <c>getarraysequence</c>.</summary>
    [Description("setarraysequence")]
    public sealed record SetArraySequenceCommand(
        string Gamearray,
        string[] Gamevars) : Command(CommandList.SetArraySequence);
}
