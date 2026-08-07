namespace BuildAssetLoader.Con
{
    // ===== Jump (deprecated) =====

    // getcurraddress <addr> — <addr> is a gamevar that receives the current address.
    public sealed record GetcurraddressCommand(string Addr) : Command(CommandList.getcurraddress);
}

