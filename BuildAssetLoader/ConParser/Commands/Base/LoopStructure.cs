// See: https://wiki.eduke32.com/wiki/Category:All_commands
namespace BuildAssetLoader.Con
{
    /// <summary>Base type for loop commands of the form <c>while* &lt;cond&gt; { ...body... }</c>. Has no explicit
    /// terminating keyword; the loop's scope is the following statement or {}-block.</summary>
    public abstract record LoopStructure(CommandList Start) : Command(Start)
    {
        public List<Command> Body { get; } = [];
    }
}
