namespace BuildAssetLoader.Con
{
    // gettimedate <sec> <min> <hour> <mday> <mon> <year> <wday> <yday> — local time/date into gamevars.
    public sealed record GettimedateCommand(
        string Sec, string Min, string Hour, string Mday, string Mon, string Year, string Wday, string Yday)
        : Command(CommandList.gettimedate);
}

