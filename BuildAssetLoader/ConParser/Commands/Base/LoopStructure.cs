namespace BuildAssetLoader.Con
{
    /// <summary>while*&lt;cond&gt; { ...body... }. No explicit end keyword.</summary>
    public abstract record LoopStructure(CommandList Start) : Command(Start)
    {
        public List<Command> Body { get; } = [];
    }
}

