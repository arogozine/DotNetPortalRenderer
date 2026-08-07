namespace BuildAssetLoader.Con
{
    /// <summary>Block-scoped command that closes on an explicit terminating keyword (enda, ends, endevent, ...).</summary>
    public abstract record Structure(CommandList Start, CommandList End) : Command(Start)
    {
        public List<Command> Body { get; } = [];
    }
}

