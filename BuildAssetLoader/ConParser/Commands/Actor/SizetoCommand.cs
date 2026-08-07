namespace BuildAssetLoader.Con
{
    // sizeto <xrepeat> <yrepeat> — gradual sprite resize; xrepeat/yrepeat are frequently defines (e.g. MAXXSTRETCH).
    public sealed record SizetoCommand(string Xrepeat, string Yrepeat) : Command(CommandList.sizeto);
}

