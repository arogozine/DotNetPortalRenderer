// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Increments the current actor's health by <c>Number</c>, which may be positive or negative. When used
    /// on a player sprite this can raise health above MAXPLAYERHEALTH.</summary>
    [Description("addstrength")]
    public sealed record AddStrengthCommand(string Number) : Command(CommandList.AddStrength);
}
