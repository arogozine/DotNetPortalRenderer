namespace BuildAssetLoader.Con
{
    // Flat commands from Commands.Flow.cs (If Components, Termination, Jump).
    // AI Assisted
    public static partial class ConTreeBuilder
    {
        private static Command? TryParseFlow(CommandList command, ConTreeCursor cursor) => command switch
        {
            CommandList.NullOp => new NullOpCommand(),

            CommandList.Break => new BreakCommand(),
            CommandList.Continue => new ContinueCommand(),
            CommandList.Exit => new ExitCommand(),
            CommandList.Return => new ReturnCommand(),
            CommandList.Terminate => new TerminateCommand(),

            CommandList.GetCurrAddress => new GetCurrAddressCommand(cursor.ReadValue()),
            CommandList.Jump => new JumpCommand(cursor.ReadValue()),

            _ => null,
        };
    }
}
