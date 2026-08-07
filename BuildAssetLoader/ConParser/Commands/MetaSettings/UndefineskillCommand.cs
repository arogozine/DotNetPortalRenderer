namespace BuildAssetLoader.Con
{
    // undefineskill <skill>
    public sealed record UndefineskillCommand(int Skill) : Command(CommandList.undefineskill);
}

