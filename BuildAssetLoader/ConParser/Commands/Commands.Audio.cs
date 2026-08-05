namespace BuildAssetLoader.Con
{
    // ===== Sounds =====

    // definesound <value> <filename> <pitch_lower> <pitch_upper> <priority> <type> <distance> [volume]
    public sealed record DefinesoundCommand(
        string Value, string Filename, int PitchLower, int PitchUpper, int Priority, int Type, int Distance, int? Volume)
        : Command(CommandList.definesound);

    // sound/soundvar <sound number> — plays a sound defined by definesound.
    public record BaseSoundCommand(CommandList Start, string SoundNumber) : Command(Start);

    public sealed record SoundCommand(string SoundNumber) : BaseSoundCommand(CommandList.sound, SoundNumber);
    public sealed record SoundvarCommand(string SoundNumber) : BaseSoundCommand(CommandList.soundvar, SoundNumber);

    // soundonce/soundoncevar <sound number> — like sound, but won't restart while an instance is still playing.
    public sealed record SoundonceCommand(string SoundNumber) : BaseSoundCommand(CommandList.soundonce, SoundNumber);
    public sealed record SoundoncevarCommand(string SoundNumber) : BaseSoundCommand(CommandList.soundoncevar, SoundNumber);

    // globalsound/globalsoundvar <sound> — audible everywhere in the map.
    public sealed record GlobalsoundCommand(string Sound) : BaseSoundCommand(CommandList.globalsound, Sound);
    public sealed record GlobalsoundvarCommand(string Sound) : BaseSoundCommand(CommandList.globalsoundvar, Sound);

    // screensound <sound#> — unconditionally plays a session-wide sound (e.g. from menus).
    public sealed record ScreensoundCommand(string Sound) : Command(CommandList.screensound);

    // stopsound/stopsoundvar <sound number>
    public sealed record StopsoundCommand(string SoundNumber) : BaseSoundCommand(CommandList.stopsound, SoundNumber);
    public sealed record StopsoundvarCommand(string SoundNumber) : BaseSoundCommand(CommandList.stopsoundvar, SoundNumber);

    // stopactorsound <sprite ID> <sound#> — stops a sound coming from one specific actor.
    public sealed record StopactorsoundCommand(string SpriteId, string Sound) : Command(CommandList.stopactorsound);

    // stopallsounds
    public sealed record StopallsoundsCommand() : Command(CommandList.stopallsounds);

    // setactorsoundpitch <actor#> <sound#> <pitchoffset>
    public sealed record SetactorsoundpitchCommand(string ActorId, string Sound, string PitchOffset) : Command(CommandList.setactorsoundpitch);

    // ===== Music =====

    // music <volume> <level 1> [level 2] ... [level MAXLEVELS] — declarative, placed outside actor/event code.
    public sealed record MusicCommand(int Volume, string[] Levels) : Command(CommandList.music);

    // starttrack/starttrackvar <track#> — changes the currently playing background music.
    public record BaseStarttrackCommand(CommandList Start, string Track) : Command(Start);

    public sealed record StarttrackCommand(string Track) : BaseStarttrackCommand(CommandList.starttrack, Track);
    public sealed record StarttrackvarCommand(string Track) : BaseStarttrackCommand(CommandList.starttrackvar, Track);

    // getmusicposition <gamevar> — implementation-specific, discouraged.
    public sealed record GetmusicpositionCommand(string Gamevar) : Command(CommandList.getmusicposition);

    // setmusicposition <gamevar> — implementation-specific, discouraged.
    public sealed record SetmusicpositionCommand(string Gamevar) : Command(CommandList.setmusicposition);

    // ===== Quotes =====

    // definequote <quote number> <quote text> — declarative, max 128 characters.
    public sealed record DefinequoteCommand(int QuoteNumber, string QuoteText) : Command(CommandList.definequote);

    // redefinequote <quote number> <quote text> — like definequote, usable inside actors/events/states.
    public sealed record RedefinequoteCommand(int QuoteNumber, string QuoteText) : Command(CommandList.redefinequote);

    // quote <quote number> — displays a quote centered at the top of the screen for ~2 seconds.
    public sealed record QuoteCommand(int QuoteNumber) : Command(CommandList.quote);

    // userquote <quote number> — adds a quote to the four-line multiplayer/chat text buffer.
    public sealed record UserquoteCommand(int QuoteNumber) : Command(CommandList.userquote);

    // qsprintf <destination quote> <source quote> <parameter 1> [...] [parameter 32] — %d/%ld/%s substitution.
    public sealed record QsprintfCommand(int DestinationQuote, int SourceQuote, string[] Parameters) : Command(CommandList.qsprintf);

    // qstrcpy <quote1> <quote2> — copies quote2's text into quote1.
    public sealed record QstrcpyCommand(int Quote1, int Quote2) : Command(CommandList.qstrcpy);

    // qstrcat <quote1> <quote2> — appends quote2's text to quote1.
    public sealed record QstrcatCommand(int Quote1, int Quote2) : Command(CommandList.qstrcat);

    // qstrncat <quote1> <quote2> <num> — appends the first <num> characters of quote2 to quote1.
    public sealed record QstrncatCommand(int Quote1, int Quote2, int Num) : Command(CommandList.qstrncat);

    // qstrlen <gamevar> <quote> — measures a quote's length into a gamevar.
    public sealed record QstrlenCommand(string Gamevar, int Quote) : Command(CommandList.qstrlen);

    // qsubstr <quote1> <quote2> <start> <length> — copies a substring of quote2 into quote1.
    public sealed record QsubstrCommand(int Quote1, int Quote2, int Start, int Length) : Command(CommandList.qsubstr);

    // qstrdim <width return var> <height return var> <tilenum> <x> <y> <zoom> <block angle> <quote> <orientation>
    //         <xspace> <yline> <xbetween> <ybetween> <text flags> <x1> <y1> <x2> <y2>
    // Calculates the on-screen dimensions of an equivalent screentext call.
    public sealed record QstrdimCommand(
        string WidthReturnVar, string HeightReturnVar, string TileNum, string X, string Y, string Zoom, string BlockAngle,
        int Quote, string Orientation, string Xspace, string Yline, string Xbetween, string Ybetween, string TextFlags,
        string X1, string Y1, string X2, string Y2)
        : Command(CommandList.qstrdim);

    // qgetsysstr <quoteID> <strID> — copies a system string (STR_MAPNAME, STR_PLAYERNAME, ...) into a quote.
    public sealed record QgetsysstrCommand(int QuoteId, string StrId) : Command(CommandList.qgetsysstr);

    // getpname <QUOTE #> <gamevar holding PLAYER ID> — copies a player's name into a quote.
    public sealed record GetpnameCommand(int QuoteNumber, string PlayerIdVar) : Command(CommandList.getpname);

    // getkeyname <quoteID> <funcID> <key> — copies a gamefunc's bound key name into a quote.
    public sealed record GetkeynameCommand(int QuoteId, string FuncId, string Key) : Command(CommandList.getkeyname);
}
