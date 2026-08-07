namespace BuildAssetLoader.Con
{
    // undefinevolume <volume>
    public sealed record UndefinevolumeCommand(int Volume) : Command(CommandList.undefinevolume);
}

