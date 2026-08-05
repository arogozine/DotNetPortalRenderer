namespace BuildAssetLoader.Con
{
    // Flat commands from Commands.Audio.cs (Sounds, Music, Quotes).
    // AI Assisted
    public static partial class ConTreeBuilder
    {
        private static Command? TryParseAudio(CommandList command, ConTreeCursor cursor) => command switch
        {
            CommandList.definesound => new DefinesoundCommand(
                cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadInt(), cursor.ReadInt(), cursor.ReadInt(), cursor.ReadInt(), cursor.ReadInt(),
                cursor.TryReadInt()),

            CommandList.sound => new SoundCommand(cursor.ReadValue()),
            CommandList.soundvar => new SoundvarCommand(cursor.ReadValue()),
            CommandList.soundonce => new SoundonceCommand(cursor.ReadValue()),
            CommandList.soundoncevar => new SoundoncevarCommand(cursor.ReadValue()),
            CommandList.globalsound => new GlobalsoundCommand(cursor.ReadValue()),
            CommandList.globalsoundvar => new GlobalsoundvarCommand(cursor.ReadValue()),
            CommandList.screensound => new ScreensoundCommand(cursor.ReadValue()),
            CommandList.stopsound => new StopsoundCommand(cursor.ReadValue()),
            CommandList.stopsoundvar => new StopsoundvarCommand(cursor.ReadValue()),
            CommandList.stopactorsound => new StopactorsoundCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.stopallsounds => new StopallsoundsCommand(),
            CommandList.setactorsoundpitch => new SetactorsoundpitchCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),

            CommandList.music => ParseMusic(cursor),
            CommandList.starttrack => new StarttrackCommand(cursor.ReadValue()),
            CommandList.starttrackvar => new StarttrackvarCommand(cursor.ReadValue()),
            CommandList.getmusicposition => new GetmusicpositionCommand(cursor.ReadValue()),
            CommandList.setmusicposition => new SetmusicpositionCommand(cursor.ReadValue()),

            CommandList.definequote => new DefinequoteCommand(cursor.ReadInt(), cursor.ReadJoinedRemainder()),
            CommandList.redefinequote => new RedefinequoteCommand(cursor.ReadInt(), cursor.ReadJoinedRemainder()),
            CommandList.quote => new QuoteCommand(cursor.ReadInt()),
            CommandList.userquote => new UserquoteCommand(cursor.ReadInt()),
            CommandList.qsprintf => ParseQsprintf(cursor),
            CommandList.qstrcpy => new QstrcpyCommand(cursor.ReadInt(), cursor.ReadInt()),
            CommandList.qstrcat => new QstrcatCommand(cursor.ReadInt(), cursor.ReadInt()),
            CommandList.qstrncat => new QstrncatCommand(cursor.ReadInt(), cursor.ReadInt(), cursor.ReadInt()),
            CommandList.qstrlen => new QstrlenCommand(cursor.ReadValue(), cursor.ReadInt()),
            CommandList.qsubstr => new QsubstrCommand(cursor.ReadInt(), cursor.ReadInt(), cursor.ReadInt(), cursor.ReadInt()),
            CommandList.qstrdim => new QstrdimCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(),
                cursor.ReadInt(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.qgetsysstr => new QgetsysstrCommand(cursor.ReadInt(), cursor.ReadValue()),
            CommandList.getpname => new GetpnameCommand(cursor.ReadInt(), cursor.ReadValue()),
            CommandList.getkeyname => new GetkeynameCommand(cursor.ReadInt(), cursor.ReadValue(), cursor.ReadValue()),

            _ => null,
        };

        private static MusicCommand ParseMusic(ConTreeCursor cursor)
        {
            int volume = cursor.ReadInt();
            return new MusicCommand(volume, cursor.ReadAllContiguousValues());
        }

        private static QsprintfCommand ParseQsprintf(ConTreeCursor cursor)
        {
            int destination = cursor.ReadInt();
            int source = cursor.ReadInt();
            return new QsprintfCommand(destination, source, cursor.ReadAllContiguousValues());
        }
    }
}
