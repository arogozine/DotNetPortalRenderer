namespace BuildAssetLoader.Con
{
    // Flat commands from Commands.Screen.cs. EventLoadActorCommand is a Structure and is built in
    // ConTreeBuilder.cs (ParseEventloadactorStructure) instead.
    // AI Assisted
    public static partial class ConTreeBuilder
    {
        private static Command? TryParseScreen(CommandList command, ConTreeCursor cursor) => command switch
        {
            CommandList.StartCutscene => new StartCutsceneCommand(cursor.ReadInt()),
            CommandList.Screen => new ScreenCommand(),

            CommandList.PalFrom => ParsePalfrom(cursor),
            CommandList.GUniqHudId => new GUniqHudIdCommand(cursor.ReadValue()),
            CommandList.SetGamePalette => new SetGamePaletteCommand(cursor.ReadValue()),
            CommandList.SetAspect => new SetAspectCommand(cursor.ReadValue(), cursor.ReadValue()),

            CommandList.WackPlayer => new WackPlayerCommand(),
            CommandList.Quake => new QuakeCommand(cursor.ReadValue()),
            CommandList.PKick => new PKickCommand(),
            CommandList.PStomp => new PStompCommand(),
            CommandList.Tip => new TipCommand(),

            CommandList.RotateSprite => new RotateSpriteCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue()),
            CommandList.RotateSprite16 => new RotateSprite16Command(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue()),
            CommandList.RotateSpriteA => new RotateSpriteACommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),

            CommandList.ScreenText => new ScreenTextCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadInt(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.GameText => new GameTextCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadInt(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.GameTextZ => new GameTextZCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadInt(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue()),
            CommandList.MiniText => new MiniTextCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadInt(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.DigitalNumber => new DigitalNumberCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue()),
            CommandList.DigitalNumberZ => new DigitalNumberZCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue()),
            CommandList.ShowView => new ShowViewCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.ShowViewUnbiased => new ShowViewUnbiasedCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),

            CommandList.DisplayRand => new DisplayRandCommand(cursor.ReadValue()),
            CommandList.DisplayRandVar => new DisplayRandVarCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.DisplayRandVarVar => new DisplayRandVarVarCommand(cursor.ReadValue(), cursor.ReadValue()),

            CommandList.GetTicks => new GetTicksCommand(cursor.ReadValue()),
            CommandList.GetTimeDate => new GetTimeDateCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),

            CommandList.ActivateCheat => new ActivateCheatCommand(cursor.ReadValue()),
            CommandList.StartLevel => new StartLevelCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.InitTimer => new InitTimerCommand(cursor.ReadInt()),
            CommandList.EndOfGame => new EndOfGameCommand(cursor.ReadInt()),
            CommandList.EndOfLevel => new EndOfLevelCommand(cursor.ReadInt()),
            CommandList.CMenu => new CMenuCommand(cursor.ReadValue()),

            CommandList.Save => new SaveCommand(cursor.ReadValue()),
            CommandList.SaveNn => new SaveNnCommand(cursor.ReadValue()),

            CommandList.LoadMapState => new LoadMapStateCommand(),
            CommandList.SaveMapState => new SaveMapStateCommand(),
            CommandList.ClearMapState => new ClearMapStateCommand(cursor.ReadValue()),

            CommandList.Debug => new DebugCommand(cursor.ReadValue()),
            CommandList.AddLog => new AddLogCommand(cursor.ReadValue()),
            CommandList.AddLogVar => new AddLogVarCommand(cursor.ReadValue()),
            CommandList.Echo => new EchoCommand(cursor.ReadInt()),

            CommandList.BetaName => new BetaNameCommand(cursor.ReadValue()),
            CommandList.Enhanced => new EnhancedCommand(cursor.ReadInt()),
            CommandList.Time => new TimeCommand(cursor.ReadValue()),
            CommandList.ShadeTo => new ShadeToCommand(cursor.ReadValue()),

            CommandList.Myos => new MyosCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.MyosX => new MyosXCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.MyosPal => new MyosPalCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.MyosPalX => new MyosPalXCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),

            _ => null,
        };

        private static PalFromCommand ParsePalfrom(ConTreeCursor cursor)
        {
            int intensity = cursor.ReadInt();
            return new PalFromCommand(intensity, cursor.TryReadInt(), cursor.TryReadInt(), cursor.TryReadInt());
        }
    }
}
