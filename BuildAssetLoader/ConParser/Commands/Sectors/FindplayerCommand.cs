namespace BuildAssetLoader.Con
{
    public sealed record FindplayerCommand(string Gamevar) : BaseFindplayerCommand(CommandList.findplayer, Gamevar);
}

