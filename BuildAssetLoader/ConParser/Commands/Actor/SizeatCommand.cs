namespace BuildAssetLoader.Con
{
    // sizeat <xrepeat> <yrepeat> — instantaneous sprite resize; xrepeat/yrepeat are frequently defines (e.g. MINXSTRETCH).
    public sealed record SizeatCommand(string Xrepeat, string Yrepeat) : Command(CommandList.sizeat);
}

