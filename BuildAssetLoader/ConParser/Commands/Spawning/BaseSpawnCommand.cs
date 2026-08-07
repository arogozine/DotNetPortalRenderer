// See: https://wiki.eduke32.com/wiki/Category:All_commands

namespace BuildAssetLoader.Con
{
    // ===== Spawning =====

    /// <summary>Shared base for the spawn/espawn/qspawn/eqspawn family: places a new actor of the given tile number
    /// at the spawning actor's position. An "e" prefix sets gamevar RETURN to the new sprite's id; a "q" prefix
    /// inserts it into the decal deletion queue; a "var" suffix takes a gamevar rather than a constant/define for
    /// the tile number. Synthetic base class used to share logic between the CON commands in this codebase; not
    /// itself a CON keyword.</summary>
    public record BaseSpawnCommand(CommandList Start, string TileNumber) : Command(Start);
}
