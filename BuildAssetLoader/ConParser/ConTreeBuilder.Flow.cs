namespace BuildAssetLoader.Con
{
    // Flat commands from Commands.Flow.cs (If Components, Termination, Jump).
    // AI Assisted
    public static partial class ConTreeBuilder
    {
        private static Command? TryParseFlow(CommandList command, ConTreeCursor cursor) => command switch
        {
            CommandList.nullop => new NullopCommand(),

            CommandList.break_ => new BreakCommand(),
            CommandList.continue_ => new ContinueCommand(),
            CommandList.exit => new ExitCommand(),
            CommandList.return_ => new ReturnCommand(),
            CommandList.terminate => new TerminateCommand(),

            CommandList.getcurraddress => new GetcurraddressCommand(cursor.ReadValue()),
            CommandList.jump => new JumpCommand(cursor.ReadValue()),

            _ => null,
        };
    }
}
