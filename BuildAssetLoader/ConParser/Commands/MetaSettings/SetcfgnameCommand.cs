namespace BuildAssetLoader.Con
{
    // setcfgname <cfg_name>
    public sealed record SetcfgnameCommand(string CfgName) : Command(CommandList.setcfgname);
}

