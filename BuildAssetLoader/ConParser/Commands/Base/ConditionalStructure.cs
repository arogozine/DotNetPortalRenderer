// See: https://wiki.eduke32.com/wiki/Category:All_commands
namespace BuildAssetLoader.Con
{
    /// <summary>Base type for conditional commands of the form <c>if&lt;cond&gt; &lt;true-branch&gt; [else
    /// &lt;false-branch&gt;]</c>, where each branch is either one statement or a {}-block. Has no explicit
    /// terminating keyword.</summary>
    public abstract record ConditionalStructure(CommandList Start) : Command(Start)
    {
        public List<Command> Body { get; } = [];
        public List<Command>? ElseBody { get; set; }
    }
}
