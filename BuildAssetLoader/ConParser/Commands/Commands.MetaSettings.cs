namespace BuildAssetLoader.Con
{
    // ===== Meta-Settings =====

    // dynamicremap — lone keyword, enables the dynamic tile remapping system.
    public sealed record DynamicremapCommand() : Command(CommandList.dynamicremap);

    // dynamicsoundremap — lone keyword, enables the dynamic sound remapping system.
    public sealed record DynamicsoundremapCommand() : Command(CommandList.dynamicsoundremap);

    // setcfgname <cfg_name>
    public sealed record SetcfgnameCommand(string CfgName) : Command(CommandList.setcfgname);

    // setdefname <name>
    public sealed record SetdefnameCommand(string Name) : Command(CommandList.setdefname);

    // setgamename <name>
    public sealed record SetgamenameCommand(string Name) : Command(CommandList.setgamename);

    // precache <tile0> <tile1> <flag>
    public sealed record PrecacheCommand(int Tile0, int Tile1, int Flag) : Command(CommandList.precache);

    // scriptsize <integer>
    public sealed record ScriptsizeCommand(int Size) : Command(CommandList.scriptsize);

    // cheatkeys <scan code> <scan code>
    public sealed record CheatkeysCommand(int ScanCode1, int ScanCode2) : Command(CommandList.cheatkeys);

    // definecheat <cheat number> <text to activate cheat>
    public sealed record DefinecheatCommand(int CheatNumber, string ActivationText) : Command(CommandList.definecheat);

    // definegamefuncname <function> <name>
    public sealed record DefinegamefuncnameCommand(int Function, string Name) : Command(CommandList.definegamefuncname);

    // definegametype <gametypenum> <flags> <name>
    public sealed record DefinegametypeCommand(int GameTypeNum, int Flags, string Name) : Command(CommandList.definegametype);

    // definevolumename <episode number> <name>
    public sealed record DefinevolumenameCommand(int EpisodeNumber, string Name) : Command(CommandList.definevolumename);

    // definevolumeflags <vol> <flags>
    public sealed record DefinevolumeflagsCommand(int Volume, int Flags) : Command(CommandList.definevolumeflags);

    // definelevelname <episode> <levelnum> <mapname> <partime> <3dr> <levname>
    // partime/3dr are MM:SS-formatted clock strings (e.g. "01:45"), not integers.
    public sealed record DefinelevelnameCommand(int Episode, int LevelNum, string MapName, string ParTime, string DesignerTime, string LevelName)
        : Command(CommandList.definelevelname);

    // defineskillname <skill> <name>
    public sealed record DefineskillnameCommand(int Skill, string Name) : Command(CommandList.defineskillname);

    // undefinevolume <volume>
    public sealed record UndefinevolumeCommand(int Volume) : Command(CommandList.undefinevolume);

    // undefinelevel <volume> <level>
    public sealed record UndefinelevelCommand(int Volume, int Level) : Command(CommandList.undefinelevel);

    // undefineskill <skill>
    public sealed record UndefineskillCommand(int Skill) : Command(CommandList.undefineskill);

    // gamestartup <param1> <param2> ... <paramN> — 26 (v1.3D) or 30 (v1.5) startup parameters.
    public sealed record GamestartupCommand(string[] Parameters) : Command(CommandList.gamestartup);

    // ===== Meta-Settings - If (ConditionalStructure, no args) =====

    // ifrespawn { ... } [else { ... }]
    public sealed record IfrespawnCommand() : ConditionalStructure(CommandList.ifrespawn);

    // ifmultiplayer { ... } [else { ... }]
    public sealed record IfmultiplayerCommand() : ConditionalStructure(CommandList.ifmultiplayer);

    // ifclient { ... } [else { ... }]
    public sealed record IfclientCommand() : ConditionalStructure(CommandList.ifclient);

    // ifserver { ... } [else { ... }]
    public sealed record IfserverCommand() : ConditionalStructure(CommandList.ifserver);
}
