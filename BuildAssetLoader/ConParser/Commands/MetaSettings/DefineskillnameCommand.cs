namespace BuildAssetLoader.Con
{
    // defineskillname <skill> <name>
    public sealed record DefineskillnameCommand(int Skill, string Name) : Command(CommandList.defineskillname);
}

