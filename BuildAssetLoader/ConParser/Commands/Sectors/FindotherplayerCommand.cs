namespace BuildAssetLoader.Con
{
    public sealed record FindotherplayerCommand(string Gamevar) : BaseFindplayerCommand(CommandList.findotherplayer, Gamevar);
}

