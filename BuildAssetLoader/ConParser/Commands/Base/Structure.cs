// See: https://wiki.eduke32.com/wiki/Category:All_commands
namespace BuildAssetLoader.Con
{
    /// <summary>Base type for block-scoped commands that close on an explicit terminating keyword
    /// (enda, ends, endevent, ...) rather than on indentation or braces.</summary>
    public abstract record Structure(CommandList Start, CommandList End) : Command(Start)
    {
        public List<Command> Body { get; } = [];
    }
}
