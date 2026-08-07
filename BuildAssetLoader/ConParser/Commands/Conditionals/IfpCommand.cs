// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Checks player-state cases relative to the current player. Conditions is one or more predefined p*
    /// flag names (e.g. pstanding, pwalking, prunning, pducking, pfalling, pjumping, phigher, pwalkingback,
    /// prunningback, pkicking, pshrunk, pjetpack, ponsteroids, ponground, palive, pdead, pfacing); true if any of
    /// them match (logical OR).</summary>
    [Description("ifp")]
    public sealed record IfPCommand(
        string[] Conditions) : ConditionalStructure(CommandList.IfP);
}
