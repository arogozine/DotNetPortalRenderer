// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Clears a specific map's cached state (indexed as VOLUME*MAXLEVELS+LEVEL) from the map cache used
    /// by <c>savemapstate</c>/<c>loadmapstate</c>. <c>Level</c> identifies the map to clear.</summary>
    [Description("clearmapstate")]
    public sealed record ClearMapStateCommand(
        string Level) : Command(CommandList.ClearMapState);
}
