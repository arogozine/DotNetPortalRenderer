namespace BuildAssetLoader.Con
{
    // undefinelevel <volume> <level>
    public sealed record UndefinelevelCommand(int Volume, int Level) : Command(CommandList.undefinelevel);
}

