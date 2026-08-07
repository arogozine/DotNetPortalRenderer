namespace BuildAssetLoader.Con
{
    /// <summary>if&lt;cond&gt; &lt;true-branch&gt; [else &lt;false-branch&gt;]; branch is either one statement or a {}-block. No explicit end keyword.</summary>
    public abstract record ConditionalStructure(CommandList Start) : Command(Start)
    {
        public List<Command> Body { get; } = [];
        public List<Command>? ElseBody { get; set; }
    }
}

