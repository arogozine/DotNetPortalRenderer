// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Moves a sector in the X/Y plane (but not Z) using the given sprite's xvel/yvel, as used by the game
    /// engine for earthquakes, rotating sectors, swinging doors, and escalators. The resulting rotation angle can
    /// be observed via EVENT_MOVESECTOR.</summary>
    [Description("movesector")]
    public sealed record MoveSectorCommand(string SpriteId) : Command(CommandList.MoveSector);
}
