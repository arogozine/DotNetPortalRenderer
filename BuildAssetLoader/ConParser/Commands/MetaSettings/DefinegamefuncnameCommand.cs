namespace BuildAssetLoader.Con
{
    // definegamefuncname <function> <name>
    public sealed record DefinegamefuncnameCommand(int Function, string Name) : Command(CommandList.definegamefuncname);
}

