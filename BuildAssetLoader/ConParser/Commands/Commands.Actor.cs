namespace BuildAssetLoader.Con
{
    // ===== Actors - Structures (remainder) =====

    // count <number> — sets the actor's count (incremented once per actor code cycle). <number> is commonly a define.
    public sealed record CountCommand(string Number) : Command(CommandList.count);

    // resetactioncount — resets actioncount to 0, restarting the current action.
    public sealed record ResetactioncountCommand() : Command(CommandList.resetactioncount);

    // resetcount — resets the actor's count to 0.
    public sealed record ResetcountCommand() : Command(CommandList.resetcount);

    // cstat <number> — sets the sprite's cstat bitfield (often a CSTAT_SPRITE_* define).
    public sealed record CstatCommand(string Value) : Command(CommandList.cstat);

    // cstator <number> — bitwise-ORs <number> into the sprite's existing cstat.
    public sealed record CstatorCommand(string Value) : Command(CommandList.cstator);

    // clipdist <number> — sets the actor's clipping sphere radius.
    public sealed record ClipdistCommand(string Number) : Command(CommandList.clipdist);

    // sizeat <xrepeat> <yrepeat> — instantaneous sprite resize; xrepeat/yrepeat are frequently defines (e.g. MINXSTRETCH).
    public sealed record SizeatCommand(string Xrepeat, string Yrepeat) : Command(CommandList.sizeat);

    // sizeto <xrepeat> <yrepeat> — gradual sprite resize; xrepeat/yrepeat are frequently defines (e.g. MAXXSTRETCH).
    public sealed record SizetoCommand(string Xrepeat, string Yrepeat) : Command(CommandList.sizeto);

    // strength <number> — sets the actor's health; commonly a define (e.g. MYENEMY_NORMAL_STRENGTH, TOUGH).
    public sealed record StrengthCommand(string Number) : Command(CommandList.strength);

    // addstrength <number> — adjusts the actor's health by a signed delta; commonly a define.
    public sealed record AddstrengthCommand(string Number) : Command(CommandList.addstrength);

    // spritepal <number> — changes the actor's palette reference number.
    public sealed record SpritepalCommand(string Number) : Command(CommandList.spritepal);

    // getlastpal — restores the actor's palette to what it was prior to the last spritepal.
    public sealed record GetlastpalCommand() : Command(CommandList.getlastpal);

    // sleeptime <count> — sets the actor's sleep counter.
    public sealed record SleeptimeCommand(string Count) : Command(CommandList.sleeptime);

    // spriteflags <picnum> <value> (outside actor code, per-tile) or spriteflags <value> (inside actor code, per-sprite).
    // <value> is a bitfield, commonly an SFLAG_* define.
    public sealed record SpriteflagsCommand(string? Picnum, string Value) : Command(CommandList.spriteflags);

    // angoff <value> — sets the 3D model angle offset from a constant.
    public sealed record AngoffCommand(string Value) : Command(CommandList.angoff);

    // angoffvar <value> — sets the 3D model angle offset from a gamevar.
    public sealed record AngoffvarCommand(string Value) : Command(CommandList.angoffvar);

    // changespritesect <actorid> <sectnum>
    public sealed record ChangespritesectCommand(string ActorId, string Sectnum) : Command(CommandList.changespritesect);

    // changespritestat <sprite id> <statnum>
    public sealed record ChangespritestatCommand(string SpriteId, string Statnum) : Command(CommandList.changespritestat);

    // setsprite <spriteid> <x> <y> <z> — moves a sprite directly to a position, updating its sector.
    public sealed record SetspriteCommand(string SpriteId, string X, string Y, string Z) : Command(CommandList.setsprite);

    // ===== Commands =====

    // fall — begins the actor falling under gravity; also applies impact damage on landing.
    public sealed record FallCommand() : Command(CommandList.fall);

    // insertspriteq — inserts the current actor into the decal deletion queue.
    public sealed record InsertspriteqCommand() : Command(CommandList.insertspriteq);

    // killit — deletes the current actor from the map; halts further execution like return.
    public sealed record KillitCommand() : Command(CommandList.killit);

    // movesprite <sprite id> <xvel> <yvel> <zvel> <clipmask> <returnvar>
    public sealed record MovespriteCommand(string SpriteId, string Xvel, string Yvel, string Zvel, string Clipmask, string ReturnVar)
        : Command(CommandList.movesprite);

    // ssp <sprite1> <clipmask> — applies the sprite's own xvel/zvel via movesprite.
    public sealed record SspCommand(string Sprite1, string Clipmask) : Command(CommandList.ssp);

    // clipmove <return> <x> <y> <z> <sectnum> <xvect> <yvect> <walldist> <flordist> <ceildist> <clipmask>
    public sealed record ClipmoveCommand(
        string Return, string X, string Y, string Z, string Sectnum,
        string Xvect, string Yvect, string Walldist, string Flordist, string Ceildist, string Clipmask)
        : Command(CommandList.clipmove);

    // clipmovenoslide <return> <x> <y> <z> <sectnum> <xvect> <yvect> <walldist> <flordist> <ceildist> <clipmask>
    public sealed record ClipmovenoslideCommand(
        string Return, string X, string Y, string Z, string Sectnum,
        string Xvect, string Yvect, string Walldist, string Flordist, string Ceildist, string Clipmask)
        : Command(CommandList.clipmovenoslide);

    // ===== Measurements =====

    // dist <gamevar> <sprite1> <sprite2> — 3D distance between two sprites.
    public sealed record DistCommand(string Gamevar, string Sprite1, string Sprite2) : Command(CommandList.dist);

    // ldist <gamevar> <sprite1> <sprite2> — 2D (x/y only) distance between two sprites.
    public sealed record LdistCommand(string Gamevar, string Sprite1, string Sprite2) : Command(CommandList.ldist);

    // cansee <x1> <y1> <z1> <sect1> <x2> <y2> <z2> <sect2> <returnvar>
    public sealed record CanseeCommand(
        string X1, string Y1, string Z1, string Sect1,
        string X2, string Y2, string Z2, string Sect2, string ReturnVar)
        : Command(CommandList.cansee);

    // canseespr <spriteID1> <spriteID2> <returnvar>
    public sealed record CanseesprCommand(string SpriteId1, string SpriteId2, string ReturnVar) : Command(CommandList.canseespr);

    // ===== Surroundings - Commands =====

    // hitradius <radius> <1> <2> <3> <4> — constant/define-driven radius damage (e.g. WEAKEST, WEAK, MEDIUMSTRENGTH, TOUGH).
    public sealed record HitradiusCommand(string Radius, string Damage1, string Damage2, string Damage3, string Damage4)
        : Command(CommandList.hitradius);

    // hitradiusvar <radius> <1> <2> <3> <4> — gamevar-driven radius damage.
    public sealed record HitradiusVarCommand(string Radius, string Damage1, string Damage2, string Damage3, string Damage4)
        : Command(CommandList.hitradiusvar);

    // flash — clears the level's visibility briefly and darkens the sprite, like EXPLOSION2.
    public sealed record FlashCommand() : Command(CommandList.flash);

    // ===== Mapping Features =====

    // mikesnd — plays the sound numbered by the executing actor's yvel.
    public sealed record MikesndCommand() : Command(CommandList.mikesnd);

    // respawnhitag — activates respawn sprites whose lotag matches the current actor's hitag.
    public sealed record RespawnhitagCommand() : Command(CommandList.respawnhitag);

    // ===== Player Interaction =====

    // getangletotarget <returnvar> — angle to face the actor's last-known target position.
    public sealed record GetangletotargetCommand(string ReturnVar) : Command(CommandList.getangletotarget);
}
