// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Adds (or, with a negative value, subtracts) health/strength from the player. Health cannot exceed
    /// MAXPLAYERHEALTH via this command, except when called from the ATOMICHEALTH actor (tilenum 100) where it may
    /// reach twice that. Also interrupts the player's viewscreen camera view if one is active.</summary>
    [Description("addphealth")]
    public sealed record AddPHealthCommand(
        string Amount) : Command(CommandList.AddPHealth);
}
