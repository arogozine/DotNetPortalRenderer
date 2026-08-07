// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Finds the id of the sector containing point (<c>InX</c>, <c>InY</c>, <c>InZ</c>) and writes it into
    /// <c>InOutSectnum</c>, which must be pre-seeded with an assumed-correct sector as a search hint. Returns -1 if
    /// the point is not in a valid sector.</summary>
    [Description("updatesectorz")]
    public sealed record UpdateSectorZCommand(
        string InX,
        string InY,
        string InZ,
        string InOutSectnum) : Command(CommandList.UpdateSectorZ);
}
