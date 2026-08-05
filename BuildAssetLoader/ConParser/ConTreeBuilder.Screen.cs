namespace BuildAssetLoader.Con
{
    // Flat commands from Commands.Screen.cs. EventloadactorCommand is a Structure and is built in
    // ConTreeBuilder.cs (ParseEventloadactorStructure) instead.
    // AI Assisted
    public static partial class ConTreeBuilder
    {
        private static Command? TryParseScreen(CommandList command, ConTreeCursor cursor) => command switch
        {
            CommandList.startcutscene => new StartcutsceneCommand(cursor.ReadInt()),
            CommandList.Screen => new ScreenCommand(),

            CommandList.palfrom => ParsePalfrom(cursor),
            CommandList.guniqhudid => new GuniqhudidCommand(cursor.ReadValue()),
            CommandList.setgamepalette => new SetgamepaletteCommand(cursor.ReadValue()),
            CommandList.setaspect => new SetaspectCommand(cursor.ReadValue(), cursor.ReadValue()),

            CommandList.wackplayer => new WackplayerCommand(),
            CommandList.quake => new QuakeCommand(cursor.ReadValue()),
            CommandList.pkick => new PkickCommand(),
            CommandList.pstomp => new PstompCommand(),
            CommandList.tip => new TipCommand(),

            CommandList.rotatesprite => new RotatespriteCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue()),
            CommandList.rotatesprite16 => new Rotatesprite16Command(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue()),
            CommandList.rotatespritea => new RotatespriteaCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),

            CommandList.screentext => new ScreentextCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadInt(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.gametext => new GametextCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadInt(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.gametextz => new GametextzCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadInt(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue()),
            CommandList.minitext => new MinitextCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadInt(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.digitalnumber => new DigitalnumberCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue()),
            CommandList.digitalnumberz => new DigitalnumberzCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue()),
            CommandList.showview => new ShowviewCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.showviewunbiased => new ShowviewunbiasedCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),

            CommandList.displayrand => new DisplayrandCommand(cursor.ReadValue()),
            CommandList.displayrandvar => new DisplayrandvarCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.displayrandvarvar => new DisplayrandvarvarCommand(cursor.ReadValue(), cursor.ReadValue()),

            CommandList.getticks => new GetticksCommand(cursor.ReadValue()),
            CommandList.gettimedate => new GettimedateCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),

            CommandList.activatecheat => new ActivatecheatCommand(cursor.ReadValue()),
            CommandList.startlevel => new StartlevelCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.inittimer => new InittimerCommand(cursor.ReadInt()),
            CommandList.endofgame => new EndofgameCommand(cursor.ReadInt()),
            CommandList.endoflevel => new EndoflevelCommand(cursor.ReadInt()),
            CommandList.cmenu => new CmenuCommand(cursor.ReadValue()),

            CommandList.save => new SaveCommand(cursor.ReadValue()),
            CommandList.savenn => new SavennCommand(cursor.ReadValue()),

            CommandList.loadmapstate => new LoadmapstateCommand(),
            CommandList.savemapstate => new SavemapstateCommand(),
            CommandList.clearmapstate => new ClearmapstateCommand(cursor.ReadValue()),

            CommandList.debug => new DebugCommand(cursor.ReadValue()),
            CommandList.addlog => new AddlogCommand(cursor.ReadValue()),
            CommandList.addlogvar => new AddlogvarCommand(cursor.ReadValue()),
            CommandList.echo => new EchoCommand(cursor.ReadInt()),

            CommandList.betaname => new BetanameCommand(cursor.ReadValue()),
            CommandList.enhanced => new EnhancedCommand(cursor.ReadInt()),
            CommandList.time => new TimeCommand(cursor.ReadValue()),
            CommandList.shadeto => new ShadetoCommand(cursor.ReadValue()),

            CommandList.myos => new MyosCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.myosx => new MyosxCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.myospal => new MyospalCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.myospalx => new MyospalxCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),

            _ => null,
        };

        private static PalfromCommand ParsePalfrom(ConTreeCursor cursor)
        {
            int intensity = cursor.ReadInt();
            return new PalfromCommand(intensity, cursor.TryReadInt(), cursor.TryReadInt(), cursor.TryReadInt());
        }
    }
}
