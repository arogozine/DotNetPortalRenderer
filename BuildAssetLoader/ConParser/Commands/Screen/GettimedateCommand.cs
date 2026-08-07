// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Gets the local time and date into gamevars: <c>Sec</c> [0,60], <c>Min</c> [0,59], <c>Hour</c>
    /// [0,23], <c>Mday</c> (day of month) [1,31], <c>Mon</c> (months since January) [0,11], <c>Year</c> (years
    /// since 0 A.D.), <c>Wday</c> (days since Sunday) [0,6], and <c>Yday</c> (days since January 1) [0,365].</summary>
    [Description("gettimedate")]
    public sealed record GetTimeDateCommand(
        string Sec,
        string Min,
        string Hour,
        string Mday,
        string Mon,
        string Year,
        string Wday,
        string Yday)
        : Command(CommandList.GetTimeDate);
}
