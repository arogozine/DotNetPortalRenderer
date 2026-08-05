namespace BuildAssetLoader.Con
{
    // Flat commands from Commands.MetaSettings.cs (Meta-Settings). The "If" members of that file
    // (ifrespawn, ifmultiplayer, ifclient, ifserver) are ConditionalStructure and live in
    // ConTreeBuilder.Conditionals.cs instead.
    // AI Assisted
    public static partial class ConTreeBuilder
    {
        private static Command? TryParseMetaSettings(CommandList command, ConTreeCursor cursor) => command switch
        {
            CommandList.dynamicremap => new DynamicremapCommand(),
            CommandList.dynamicsoundremap => new DynamicsoundremapCommand(),
            CommandList.setcfgname => new SetcfgnameCommand(cursor.ReadValue()),
            CommandList.setdefname => new SetdefnameCommand(cursor.ReadValue()),
            CommandList.setgamename => new SetgamenameCommand(cursor.ReadValue()),
            CommandList.precache => new PrecacheCommand(cursor.ReadInt(), cursor.ReadInt(), cursor.ReadInt()),
            CommandList.scriptsize => new ScriptsizeCommand(cursor.ReadInt()),
            CommandList.cheatkeys => new CheatkeysCommand(cursor.ReadInt(), cursor.ReadInt()),
            CommandList.definecheat => new DefinecheatCommand(cursor.ReadInt(), cursor.ReadJoinedRemainder()),
            CommandList.definegamefuncname => new DefinegamefuncnameCommand(cursor.ReadInt(), cursor.ReadJoinedRemainder()),
            CommandList.definegametype => new DefinegametypeCommand(cursor.ReadInt(), cursor.ReadInt(), cursor.ReadJoinedRemainder()),
            CommandList.definevolumename => new DefinevolumenameCommand(cursor.ReadInt(), cursor.ReadJoinedRemainder()),
            CommandList.definevolumeflags => new DefinevolumeflagsCommand(cursor.ReadInt(), cursor.ReadInt()),
            CommandList.definelevelname => new DefinelevelnameCommand(
                cursor.ReadInt(), cursor.ReadInt(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadJoinedRemainder()),
            CommandList.defineskillname => new DefineskillnameCommand(cursor.ReadInt(), cursor.ReadJoinedRemainder()),
            CommandList.undefinevolume => new UndefinevolumeCommand(cursor.ReadInt()),
            CommandList.undefinelevel => new UndefinelevelCommand(cursor.ReadInt(), cursor.ReadInt()),
            CommandList.undefineskill => new UndefineskillCommand(cursor.ReadInt()),
            CommandList.gamestartup => new GamestartupCommand(cursor.ReadAllContiguousValues()),

            _ => null,
        };
    }
}
