namespace BuildAssetLoader.Con
{
    // ===== Cutscenes =====

    // startcutscene <cutscene path> — argument is a quote id holding the cutscene path.
    public sealed record StartcutsceneCommand(int QuoteId) : Command(CommandList.startcutscene);

    // Screen — no CommandInfo/*.html page exists for this CommandList entry (likely a reserved/quick-access
    // struct name in the wiki's own grouping rather than a documented statement). Modeled as an empty
    // placeholder pending documentation; revisit if a concrete grammar is found.
    public sealed record ScreenCommand() : Command(CommandList.Screen);

    // ===== Screen Manipulation =====

    // palfrom <intensity> <red> <green> <blue> — flashes the screen a color; red/green/blue default to 0 if omitted.
    public sealed record PalfromCommand(int Intensity, int? Red, int? Green, int? Blue) : Command(CommandList.palfrom);

    // guniqhudid <slotID> — selects the animation-state slot (0-254) used for HUD models drawn via rotatesprite.
    public sealed record GuniqhudidCommand(string SlotId) : Command(CommandList.guniqhudid);

    // setgamepalette <pal_ID> — deprecated; switches between LOOKUP.DAT base palettes (0-6).
    public sealed record SetgamepaletteCommand(string PalId) : Command(CommandList.setgamepalette);

    // setaspect <viewingrange> <yxaspect> — sets renderer field of view; only valid in screen drawing events.
    public sealed record SetaspectCommand(string ViewingRange, string YxAspect) : Command(CommandList.setaspect);

    // ===== Player Actions =====

    // wackplayer — tilts the screen as if the player was struck; resets vertical mouse aim (semi-obsolete).
    public sealed record WackplayerCommand() : Command(CommandList.wackplayer);

    // quake <count> — shakes the screen for <count> tics (26 = 1 second); also triggers lotag-33 sector effectors.
    public sealed record QuakeCommand(string Count) : Command(CommandList.quake);

    // pkick — makes the current player kick.
    public sealed record PkickCommand() : Command(CommandList.pkick);

    // pstomp — makes the nearest player look down and stomp; place in the code of the actor being stomped.
    public sealed record PstompCommand() : Command(CommandList.pstomp);

    // tip — disables the current/closest player's weapon and shows the "tip" graphic.
    public sealed record TipCommand() : Command(CommandList.tip);

    // ===== Screen Drawing =====

    // rotatesprite <x> <y> <zoom> <ang> <tilenum> <shade> <pal> <orientation> <x1> <y1> <x2> <y2>
    public sealed record RotatespriteCommand(
        string X, string Y, string Zoom, string Ang, string TileNum, string Shade, string Pal, string Orientation,
        string X1, string Y1, string X2, string Y2)
        : Command(CommandList.rotatesprite);

    // rotatesprite16 <x> <y> <z> <a> <tilenum> <shade> <pal> <orientation> <x1> <y1> <x2> <y2> — deprecated, 65536x precision.
    public sealed record Rotatesprite16Command(
        string X, string Y, string Z, string A, string TileNum, string Shade, string Pal, string Orientation,
        string X1, string Y1, string X2, string Y2)
        : Command(CommandList.rotatesprite16);

    // rotatespritea <x> <y> <zoom> <ang> <tilenum> <shade> <pal> <orientation> <alpha> <x1> <y1> <x2> <y2>
    public sealed record RotatespriteaCommand(
        string X, string Y, string Zoom, string Ang, string TileNum, string Shade, string Pal, string Orientation, string Alpha,
        string X1, string Y1, string X2, string Y2)
        : Command(CommandList.rotatespritea);

    // screentext <tilenum> <x> <y> <zoom> <block angle> <character angle> <quote> <shade> <pal> <orientation>
    //            <alpha> <xspace> <yline> <xbetween> <ybetween> <text flags> <x1> <y1> <x2> <y2>
    public sealed record ScreentextCommand(
        string TileNum, string X, string Y, string Zoom, string BlockAngle, string CharacterAngle, int Quote,
        string Shade, string Pal, string Orientation, string Alpha, string Xspace, string Yline, string Xbetween,
        string Ybetween, string TextFlags, string X1, string Y1, string X2, string Y2)
        : Command(CommandList.screentext);

    // gametext <tilenum> <x> <y> <quote> <shade> <pal> <orientation> <x1> <y1> <x2> <y2>
    public sealed record GametextCommand(
        string TileNum, string X, string Y, int Quote, string Shade, string Pal, string Orientation,
        string X1, string Y1, string X2, string Y2)
        : Command(CommandList.gametext);

    // gametextz <tilenum> <x> <y> <quote> <shade> <pal> <orientation> <x1> <y1> <x2> <y2> <textscale>
    public sealed record GametextzCommand(
        string TileNum, string X, string Y, int Quote, string Shade, string Pal, string Orientation,
        string X1, string Y1, string X2, string Y2, string TextScale)
        : Command(CommandList.gametextz);

    // minitext <x> <y> <quote> <shade> <pal>
    public sealed record MinitextCommand(string X, string Y, int Quote, string Shade, string Pal) : Command(CommandList.minitext);

    // digitalnumber <tilenum> <x> <y> <number> <shade> <pal> <orientation> <x1> <y1> <x2> <y2>
    public sealed record DigitalnumberCommand(
        string TileNum, string X, string Y, string Number, string Shade, string Pal, string Orientation,
        string X1, string Y1, string X2, string Y2)
        : Command(CommandList.digitalnumber);

    // digitalnumberz <tilenum> <x> <y> <number> <shade> <pal> <orientation> <x1> <y1> <x2> <y2> <digitalscale>
    public sealed record DigitalnumberzCommand(
        string TileNum, string X, string Y, string Number, string Shade, string Pal, string Orientation,
        string X1, string Y1, string X2, string Y2, string DigitalScale)
        : Command(CommandList.digitalnumberz);

    // showview <x> <y> <z> <angle> <horiz> <sector> <scrn_x1> <scrn_y1> <scrn_x2> <scrn_y2>
    public sealed record ShowviewCommand(
        string X, string Y, string Z, string Angle, string Horiz, string Sector,
        string ScrnX1, string ScrnY1, string ScrnX2, string ScrnY2)
        : Command(CommandList.showview);

    // showviewunbiased <x> <y> <z> <angle> <horiz> <sector> <scrn_x1> <scrn_y1> <scrn_x2> <scrn_y2>
    public sealed record ShowviewunbiasedCommand(
        string X, string Y, string Z, string Angle, string Horiz, string Sector,
        string ScrnX1, string ScrnY1, string ScrnX2, string ScrnY2)
        : Command(CommandList.showviewunbiased);

    // ===== Math (display) =====

    // displayrand <gamevar> — random number in [0, 32767]; sync-safe, usable in unsynchronized (display) code.
    public sealed record DisplayrandCommand(string Gamevar) : Command(CommandList.displayrand);

    // displayrandvar <gamevar> <maxvalue_constant> — random number in [0, maxvalue].
    public sealed record DisplayrandvarCommand(string Gamevar, string MaxValue) : Command(CommandList.displayrandvar);

    // displayrandvarvar <gamevar> <maxvalue_gamevar> — like displayrandvar, but maxvalue is itself a gamevar.
    public sealed record DisplayrandvarvarCommand(string Gamevar, string MaxValue) : Command(CommandList.displayrandvarvar);

    // ===== Time Access =====

    // getticks <gamevar> — milliseconds since the game started; not synced, for visuals/profiling only.
    public sealed record GetticksCommand(string Gamevar) : Command(CommandList.getticks);

    // gettimedate <sec> <min> <hour> <mday> <mon> <year> <wday> <yday> — local time/date into gamevars.
    public sealed record GettimedateCommand(
        string Sec, string Min, string Hour, string Mday, string Mon, string Year, string Wday, string Yday)
        : Command(CommandList.gettimedate);

    // ===== Game-Changing =====

    // activatecheat <cheat_id> — singleplayer only; also fires EVENT_ACTIVATECHEAT.
    public sealed record ActivatecheatCommand(string CheatId) : Command(CommandList.activatecheat);

    // startlevel <volume> <level> — schedules a map load, bypassing the End of Level screen.
    public sealed record StartlevelCommand(string Volume, string Level) : Command(CommandList.startlevel);

    // inittimer <rate> — changes gameplay speed (default 120); usable for "bullet time" effects.
    public sealed record InittimerCommand(int Rate) : Command(CommandList.inittimer);

    // endofgame/endoflevel <number> — triggers end of episode after <number> 1/15-second time units (default 52).
    public record BaseEndofgameCommand(CommandList Start, int Number) : Command(Start);

    public sealed record EndofgameCommand(int Number) : BaseEndofgameCommand(CommandList.endofgame, Number);
    public sealed record EndoflevelCommand(int Number) : BaseEndofgameCommand(CommandList.endoflevel, Number);

    // cmenu <value> — opens a specific menu screen (see current_menu / MENU_* defines).
    public sealed record CmenuCommand(string Value) : Command(CommandList.cmenu);

    // ===== Game Saving =====

    // save/savenn <slot number> — creates a savegame in slot 0-9; savenn keeps an existing name if present.
    public record BaseSaveCommand(CommandList Start, string SlotNumber) : Command(Start);

    public sealed record SaveCommand(string SlotNumber) : BaseSaveCommand(CommandList.save, SlotNumber);
    public sealed record SavennCommand(string SlotNumber) : BaseSaveCommand(CommandList.savenn, SlotNumber);

    // ===== Hub Maps =====

    // loadmapstate — restores the current map to its last savemapstate snapshot.
    public sealed record LoadmapstateCommand() : Command(CommandList.loadmapstate);

    // savemapstate — snapshots the current map's state for a later loadmapstate.
    public sealed record SavemapstateCommand() : Command(CommandList.savemapstate);

    // clearmapstate <level> — clears a specific map from the map cache (VOLUME*MAXLEVELS+LEVEL).
    public sealed record ClearmapstateCommand(string Level) : Command(CommandList.clearmapstate);

    // ===== Debug =====

    // debug <number> — logs <number> and triggers a debugger breakpoint in non-release builds.
    public sealed record DebugCommand(string Number) : Command(CommandList.debug);

    // addlog <gamevar> — logs a gamevar/gamearray value to the console and eduke32.log.
    public sealed record AddlogCommand(string Gamevar) : Command(CommandList.addlog);

    // addlogvar <gamevar> — alias-grammar sibling of addlog.
    public sealed record AddlogvarCommand(string Gamevar) : Command(CommandList.addlogvar);

    // echo <quote number> — prints a quote to the console/log only, not to the screen.
    public sealed record EchoCommand(int QuoteNumber) : Command(CommandList.echo);

    // ===== Deprecated =====

    // betaname <string> — obsolete, never referenced.
    public sealed record BetanameCommand(string Value) : Command(CommandList.betaname);

    // enhanced <value> — obsolete CON version compatibility marker.
    public sealed record EnhancedCommand(int Value) : Command(CommandList.enhanced);

    // eventloadactor <name/tilenum> { ... } enda — runs when a matching actor is loaded into the map.
    public sealed record EventloadactorCommand(string ActorName) : Structure(CommandList.eventloadactor, CommandList.enda);

    // time <gamevar> — compiles but does nothing, like nullop.
    public sealed record TimeCommand(string Gamevar) : Command(CommandList.time);

    // shadeto <value> — dummy command in EDuke32, does nothing.
    public sealed record ShadetoCommand(string Value) : Command(CommandList.shadeto);

    // ===== Screen Drawing (deprecated myos family) =====

    // myos <x> <y> <tilenum> <shade> <orientation> — older, more limited rotatesprite; draws at 320x200.
    public sealed record MyosCommand(string X, string Y, string TileNum, string Shade, string Orientation) : Command(CommandList.myos);

    // myosx <x> <y> <tilenum> <shade> <orientation> — like myos, but drawn at half size.
    public sealed record MyosxCommand(string X, string Y, string TileNum, string Shade, string Orientation) : Command(CommandList.myosx);

    // myospal <x> <y> <tilenum> <shade> <orientation> <pal> — like myos, with an explicit palette.
    public sealed record MyospalCommand(string X, string Y, string TileNum, string Shade, string Orientation, string Pal)
        : Command(CommandList.myospal);

    // myospalx <x> <y> <tilenum> <shade> <orientation> <pal> — like myospal, but drawn at half size.
    public sealed record MyospalxCommand(string X, string Y, string TileNum, string Shade, string Orientation, string Pal)
        : Command(CommandList.myospalx);
}
