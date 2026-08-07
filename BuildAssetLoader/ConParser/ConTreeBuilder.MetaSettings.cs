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
            CommandList.DynamicRemap => new DynamicRemapCommand(),
            CommandList.DynamicSoundRemap => new DynamicSoundRemapCommand(),
            CommandList.SetCfgName => new SetCfgNameCommand(cursor.ReadValue()),
            CommandList.SetDefName => new SetDefNameCommand(cursor.ReadValue()),
            CommandList.SetGameName => new SetGameNameCommand(cursor.ReadValue()),
            CommandList.Precache => new PrecacheCommand(cursor.ReadInt(), cursor.ReadInt(), cursor.ReadInt()),
            CommandList.ScriptSize => new ScriptSizeCommand(cursor.ReadInt()),
            CommandList.CheatKeys => new CheatKeysCommand(cursor.ReadInt(), cursor.ReadInt()),
            CommandList.DefineCheat => new DefineCheatCommand(cursor.ReadInt(), cursor.ReadJoinedRemainder()),
            CommandList.DefineGameFuncName => new DefineGameFuncNameCommand(cursor.ReadInt(), cursor.ReadJoinedRemainder()),
            CommandList.DefineGameType => new DefineGameTypeCommand(cursor.ReadInt(), cursor.ReadInt(), cursor.ReadJoinedRemainder()),
            CommandList.DefineVolumeName => new DefineVolumeNameCommand(cursor.ReadInt(), cursor.ReadJoinedRemainder()),
            CommandList.DefineVolumeFlags => new DefineVolumeFlagsCommand(cursor.ReadInt(), cursor.ReadInt()),
            CommandList.DefineLevelName => new DefineLevelNameCommand(
                cursor.ReadInt(), cursor.ReadInt(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadJoinedRemainder()),
            CommandList.DefineSkillName => new DefineSkillNameCommand(cursor.ReadInt(), cursor.ReadJoinedRemainder()),
            CommandList.UndefineVolume => new UndefineVolumeCommand(cursor.ReadInt()),
            CommandList.UndefineLevel => new UndefineLevelCommand(cursor.ReadInt(), cursor.ReadInt()),
            CommandList.UndefineSkill => new UndefineSkillCommand(cursor.ReadInt()),
            CommandList.GameStartup => new GameStartupCommand(cursor.ReadAllContiguousValues()),

            _ => null,
        };
    }
}
