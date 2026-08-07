namespace BuildAssetLoader.Con
{
    // Flat commands from Commands.Audio.cs (Sounds, Music, Quotes).
    // AI Assisted
    public static partial class ConTreeBuilder
    {
        private static Command? TryParseAudio(CommandList command, ConTreeCursor cursor) => command switch
        {
            CommandList.DefineSound => new DefineSoundCommand(
                cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadInt(), cursor.ReadInt(), cursor.ReadInt(), cursor.ReadInt(), cursor.ReadInt(),
                cursor.TryReadInt()),

            CommandList.Sound => new SoundCommand(cursor.ReadValue()),
            CommandList.SoundVar => new SoundVarCommand(cursor.ReadValue()),
            CommandList.SoundOnce => new SoundOnceCommand(cursor.ReadValue()),
            CommandList.SoundOnceVar => new SoundOnceVarCommand(cursor.ReadValue()),
            CommandList.GlobalSound => new GlobalSoundCommand(cursor.ReadValue()),
            CommandList.GlobalSoundVar => new GlobalSoundVarCommand(cursor.ReadValue()),
            CommandList.ScreenSound => new ScreenSoundCommand(cursor.ReadValue()),
            CommandList.StopSound => new StopSoundCommand(cursor.ReadValue()),
            CommandList.StopSoundVar => new StopSoundVarCommand(cursor.ReadValue()),
            CommandList.StopActorSound => new StopActorSoundCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.StopAllSounds => new StopAllSoundsCommand(),
            CommandList.SetActorSoundPitch => new SetActorSoundPitchCommand(cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),

            CommandList.Music => ParseMusic(cursor),
            CommandList.StartTrack => new StartTrackCommand(cursor.ReadValue()),
            CommandList.StartTrackVar => new StartTrackVarCommand(cursor.ReadValue()),
            CommandList.GetMusicPosition => new GetMusicPositionCommand(cursor.ReadValue()),
            CommandList.SetMusicPosition => new SetMusicPositionCommand(cursor.ReadValue()),

            CommandList.DefineQuote => new DefineQuoteCommand(cursor.ReadInt(), cursor.ReadJoinedRemainder()),
            CommandList.RedefineQuote => new RedefineQuoteCommand(cursor.ReadInt(), cursor.ReadJoinedRemainder()),
            CommandList.Quote => new QuoteCommand(cursor.ReadInt()),
            CommandList.UserQuote => new UserQuoteCommand(cursor.ReadInt()),
            CommandList.QSprintf => ParseQsprintf(cursor),
            CommandList.QStrCpy => new QStrCpyCommand(cursor.ReadInt(), cursor.ReadInt()),
            CommandList.QStrCat => new QStrCatCommand(cursor.ReadInt(), cursor.ReadInt()),
            CommandList.QStrNCat => new QStrNCatCommand(cursor.ReadInt(), cursor.ReadInt(), cursor.ReadInt()),
            CommandList.QStrLen => new QStrLenCommand(cursor.ReadValue(), cursor.ReadInt()),
            CommandList.QSubStr => new QSubStrCommand(cursor.ReadInt(), cursor.ReadInt(), cursor.ReadInt(), cursor.ReadInt()),
            CommandList.QStrDim => new QStrDimCommand(
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(),
                cursor.ReadInt(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(),
                cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue(), cursor.ReadValue()),
            CommandList.QGetSysStr => new QGetSysStrCommand(cursor.ReadInt(), cursor.ReadValue()),
            CommandList.GetPName => new GetPNameCommand(cursor.ReadInt(), cursor.ReadValue()),
            CommandList.GetKeyName => new GetKeyNameCommand(cursor.ReadInt(), cursor.ReadValue(), cursor.ReadValue()),

            _ => null,
        };

        private static MusicCommand ParseMusic(ConTreeCursor cursor)
        {
            int volume = cursor.ReadInt();
            return new MusicCommand(volume, cursor.ReadAllContiguousValues());
        }

        private static QSprintfCommand ParseQsprintf(ConTreeCursor cursor)
        {
            int destination = cursor.ReadInt();
            int source = cursor.ReadInt();
            return new QSprintfCommand(destination, source, cursor.ReadAllContiguousValues());
        }
    }
}
