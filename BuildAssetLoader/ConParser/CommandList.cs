using System.ComponentModel;

namespace BuildAssetLoader.Con;

// From eDuke32 Wiki

public enum CommandList
{
	// Preprocessor

	[Description("include")]
	Include,
	[Description("includedefault")]
	IncludeDefault,
	[Description("define")]
	Define,

	// // single-line comment
	// /* block comment */
	// whitespace characters: ( ) ; ,

	// Meta-Settings

	[Description("dynamicremap")]
	DynamicRemap,
	[Description("dynamicsoundremap")]
	DynamicSoundRemap,
	[Description("setcfgname")]
	SetCfgName,
	[Description("setdefname")]
	SetDefName,
	[Description("setgamename")]
	SetGameName,
	[Description("precache")]
	Precache,
	[Description("scriptsize")]
	ScriptSize,
	[Description("cheatkeys")]
	CheatKeys,
	[Description("definecheat")]
	DefineCheat,
	[Description("definegamefuncname")]
	DefineGameFuncName,
	[Description("definegametype")]
	DefineGameType,
	[Description("definevolumename")]
	DefineVolumeName,
	[Description("definevolumeflags")]
	DefineVolumeFlags,
	[Description("definelevelname")]
	DefineLevelName,
	[Description("defineskillname")]
	DefineSkillName,
	[Description("undefinevolume")]
	UndefineVolume,
	[Description("undefinelevel")]
	UndefineLevel,
	[Description("undefineskill")]
	UndefineSkill,
	[Description("gamestartup")]
	GameStartup,

	// Meta-Settings - If

	[Description("ifrespawn")]
	IfRespawn,
	[Description("ifmultiplayer")]
	IfMultiplayer,
	[Description("ifclient")]
	IfClient,
	[Description("ifserver")]
	IfServer,

	// Global Settings

	// Global Settings - Object-Oriented
	[Description("actor")]
	Actor,
	[Description("useractor")]
	UserActor,
	[Description("enda")]
	Enda,
	// Global Settings - Procedural
	[Description("onevent")]
	OnEvent,
	[Description("appendevent")]
	AppendEvent,
	[Description("endevent")]
	EndEvent,
	// Global Settings - Subroutines
	[Description("state")]
	State,
	[Description("defstate")]
	DefState,
	[Description("ends")]
	Ends,
	[Description("prependstate")]
	PrependState,
	[Description("appendstate")]
	AppendState,

	// Flow Control,
	// If Components,
	[Description("nullop")]
	NullOp,
	[Description("else")]
	Else,

	// Switch,
	[Description("switch")]
	Switch,
	[Description("endswitch")]
	EndSwitch,
	[Description("case")]
	Case,
	[Description("default")]
	Default,

	// Termination,
	[Description("break")]
	Break,
	[Description("continue")]
	Continue,
	[Description("exit")]
	Exit,
	[Description("return")]
	Return,
	[Description("terminate")]
	Terminate,

	// Jump,
	// Note: Jumping commands are deprecated and will not be supported by Lunatic. Use loops instead.,
	[Description("getcurraddress")]
	GetCurrAddress,
	[Description("jump")]
	Jump,

	// Loops,
	[Description("whilevarl")]
	WhileVarL,
	[Description("whilevarvarl")]
	WhileVarVarL,
	[Description("whilevare")]
	WhileVarE,
	[Description("whilevarn")]
	WhileVarN,
	[Description("whilevarvarn")]
	WhileVarVarN,
	// It is also possible to loop by calling a state from within itself.,

	// Game Variables,
	[Description("gamevar")]
	GameVar,
	[Description("gamearray")]
	GameArray,

	// Gamevar Operators,
	[Description("setvar")]
	SetVar,
	[Description("setvarvar")]
	SetVarVar,
	[Description("setarray")]
	SetArray,
	[Description("addvar")]
	AddVar,
	[Description("addvarvar")]
	AddVarVar,
	[Description("subvar")]
	SubVar,
	[Description("subvarvar")]
	SubVarVar,
	[Description("mulvar")]
	MulVar,
	[Description("mulvarvar")]
	MulVarVar,
	[Description("divvar")]
	DivVar,
	[Description("divvarvar")]
	DivVarVar,
	[Description("modvar")]
	ModVar,
	[Description("modvarvar")]
	ModVarVar,
	[Description("andvar")]
	AndVar,
	[Description("andvarvar")]
	AndVarVar,
	[Description("orvar")]
	OrVar,
	[Description("orvarvar")]
	OrVarVar,
	[Description("xorvar")]
	XorVar,
	[Description("xorvarvar")]
	XorVarVar,
	[Description("randvar")]
	RandVar,
	[Description("randvarvar")]
	RandVarVar,

	// Gamevar Conditions,
	[Description("ifvare")]
	IfVarE,
	[Description("ifvarn")]
	IfVarN,
	[Description("ifvarg")]
	IfVarG,
	[Description("ifvarl")]
	IfVarL,
	[Description("ifvarand")]
	IfVarAnd,
	[Description("ifvaror")]
	IfVarOr,
	[Description("ifvarxor")]
	IfVarXor,
	[Description("ifvareither")]
	IfVarEither,
	[Description("ifvarvare")]
	IfVarVarE,
	[Description("ifvarvarn")]
	IfVarVarN,
	[Description("ifvarvarg")]
	IfVarVarG,
	[Description("ifvarvarl")]
	IfVarVarL,
	[Description("ifvarvarand")]
	IfVarVarAnd,
	[Description("ifvarvaror")]
	IfVarVarOr,
	[Description("ifvarvarxor")]
	IfVarVarXor,
	[Description("ifvarvareither")]
	IfVarVarEither,

	// Math Operations,
	[Description("sqrt")]
	Sqrt,
	[Description("calchypotenuse")]
	CalcHypotenuse,
	[Description("sin")]
	Sin,
	[Description("cos")]
	Cos,
	[Description("shiftvarl")]
	ShiftVarL,
	[Description("shiftvarr")]
	ShiftVarR,
	[Description("mulscale")]
	MulScale,
	[Description("getangle")]
	GetAngle,
	[Description("getincangle")]
	GetIncAngle,

	// Array Operations,
	[Description("getarraysize")]
	GetArraySize,
	[Description("getarraysequence")]
	GetArraySequence,
	[Description("resizearray")]
	ResizeArray,
	[Description("copy")]
	Copy,

	// setarray,
	[Description("setarraysequence")]
	SetArraySequence,

	// Data Saving,
	[Description("readgamevar")]
	ReadGameVar,
	[Description("savegamevar")]
	SaveGameVar,
	[Description("readarrayfromfile")]
	ReadArrayFromFile,
	[Description("writearraytofile")]
	WriteArrayToFile,

	// Structure Access,
	[Description("getactor")]
	GetActor,
	[Description("getactorvar")]
	GetActorVar,
	[Description("getinput")]
	GetInput,
	[Description("getplayer")]
	GetPlayer,
	[Description("getplayervar")]
	GetPlayerVar,
	[Description("getprojectile")]
	GetProjectile,
	[Description("getsector")]
	GetSector,
	[Description("getthisprojectile")]
	GetThisProjectile,
	[Description("gettspr")]
	GetTspr,
	[Description("getuserdef")]
	GetUserDef,
	[Description("getwall")]
	GetWall,
	[Description("setactor")]
	SetActor,
	[Description("setactorvar")]
	SetActorVar,
	[Description("setinput")]
	SetInput,
	[Description("setplayer")]
	SetPlayer,
	[Description("setplayervar")]
	SetPlayerVar,
	[Description("setprojectile")]
	SetProjectile,
	[Description("setsector")]
	SetSector,
	[Description("setthisprojectile")]
	SetThisProjectile,
	[Description("settspr")]
	SetTspr,
	[Description("setuserdef")]
	SetUserDef,
	[Description("setwall")]
	SetWall,

	// Actors,
	// Structures,
	[Description("cactor")]
	CActor,
	[Description("action")]
	Action,
	[Description("ai")]
	Ai,
	[Description("move")]
	Move,
	[Description("count")]
	Count,
	[Description("resetactioncount")]
	ResetActionCount,
	[Description("resetcount")]
	ResetCount,
	[Description("cstat")]
	CStat,
	[Description("cstator")]
	CStatOr,
	[Description("clipdist")]
	ClipDist,
	[Description("sizeat")]
	SizeAt,
	[Description("sizeto")]
	SizeTo,
	[Description("strength")]
	Strength,
	[Description("addstrength")]
	AddStrength,
	[Description("spritepal")]
	SpritePal,
	[Description("getlastpal")]
	GetLastPal,
	[Description("sleeptime")]
	SleepTime,
	[Description("spriteflags")]
	SpriteFlags,
	[Description("angoff")]
	AngOff,
	[Description("angoffvar")]
	AngOffVar,
	[Description("changespritesect")]
	ChangeSpriteSect,
	[Description("changespritestat")]
	ChangeSpriteStat,
	[Description("setsprite")]
	SetSprite,

	// If,
	[Description("ifactor")]
	IfActor,
	[Description("ifaction")]
	IfAction,
	[Description("ifactioncount")]
	IfActionCount,
	[Description("ifai")]
	IfAi,
	[Description("ifcount")]
	IfCount,
	[Description("ifmove")]
	IfMove,
	[Description("ifspawnedby")]
	IfSpawnedBy,
	[Description("ifspritepal")]
	IfSpritePal,
	[Description("ifstrength")]
	IfStrength,
	[Description("ifhitweapon")]
	IfHitWeapon,
	[Description("ifwasweapon")]
	IfWasWeapon,
	[Description("ifdead")]
	IfDead,
	[Description("ifactornotstayput")]
	IfActorNotStayPut,

	// Commands,
	[Description("fall")]
	Fall,
	[Description("insertspriteq")]
	InsertSpriteQ,
	[Description("killit")]
	KillIt,
	[Description("movesprite")]
	MoveSprite,
	[Description("ssp")]
	Ssp,
	[Description("clipmove")]
	ClipMove,
	[Description("clipmovenoslide")]
	ClipMoveNoSlide,

	// Measurements,
	[Description("dist")]
	Dist,
	[Description("ldist")]
	LDist,
	[Description("cansee")]
	CanSee,
	[Description("canseespr")]
	CanSeeSpr,

	// Surroundings,
	// Commands,
	[Description("hitradius")]
	HitRadius,
	[Description("hitradiusvar")]
	HitRadiusVar,
	[Description("flash")]
	Flash,

	// If,
	[Description("ifawayfromwall")]
	IfAwayFromWall,
	[Description("ifbulletnear")]
	IfBulletNear,
	[Description("ifceilingdistl")]
	IfCeilingDistL,
	[Description("iffloordistl")]
	IfFloorDistL,
	[Description("ifgapzl")]
	IfGapZL,
	[Description("ifsquished")]
	IfSquished,
	[Description("ifnotmoving")]
	IfNotMoving,
	[Description("ifinwater")]
	IfInWater,
	[Description("ifonwater")]
	IfOnWater,
	[Description("ifoutside")]
	IfOutside,
	[Description("ifinspace")]
	IfInSpace,
	[Description("ifinouterspace")]
	IfInOuterSpace,
	[Description("ifrnd")]
	IfRnd,

	// Mapping Features,
	[Description("mikesnd")]
	MikeSnd,
	[Description("respawnhitag")]
	RespawnHitag,

	// Player Interaction,
	[Description("ifangdiffl")]
	IfAngDiffL,
	[Description("ifcansee")]
	IfCanSee,
	[Description("ifcanseetarget")]
	IfCanSeeTarget,
	[Description("ifcanshoottarget")]
	IfCanShootTarget,
	[Description("ifhitspace")]
	IfHitSpace,
	[Description("getangletotarget")]
	GetAngleToTarget,

	// Spawning,
	[Description("spawn")]
	Spawn,
	[Description("espawn")]
	ESpawn,
	[Description("espawnvar")]
	ESpawnVar,
	[Description("qspawn")]
	QSpawn,
	[Description("qspawnvar")]
	QSpawnVar,
	[Description("eqspawn")]
	EqSpawn,
	[Description("eqspawnvar")]
	EqSpawnVar,

	// Materials,
	[Description("debris")]
	Debris,
	[Description("guts")]
	Guts,
	[Description("lotsofglass")]
	LotsOfGlass,
	[Description("mail")]
	Mail,
	[Description("money")]
	Money,
	[Description("paper")]
	Paper,

	// Projectiles,
	[Description("defineprojectile")]
	DefineProjectile,
	[Description("shoot")]
	Shoot,
	[Description("shootvar")]
	ShootVar,
	[Description("eshoot")]
	EShoot,
	[Description("eshootvar")]
	EShootVar,
	[Description("zshoot")]
	ZShoot,
	[Description("zshootvar")]
	ZShootVar,
	[Description("ezshoot")]
	EZShoot,
	[Description("ezshootvar")]
	EZShootVar,

	// Player,
	// Commands,
	[Description("addammo")]
	AddAmmo,
	[Description("addinventory")]
	AddInventory,
	[Description("addweapon")]
	AddWeapon,
	[Description("addweaponvar")]
	AddWeaponVar,
	[Description("addphealth")]
	AddPHealth,
	[Description("tossweapon")]
	TossWeapon,
	[Description("gmaxammo")]
	GMaxAmmo,
	[Description("smaxammo")]
	SMaxAmmo,
	[Description("checkavailinven")]
	CheckAvailInven,
	[Description("checkavailweapon")]
	CheckAvailWeapon,
	[Description("addkills")]
	AddKills,
	[Description("lockplayer")]
	LockPlayer,
	[Description("resetplayer")]
	ResetPlayer,
	[Description("resetplayerflags")]
	ResetPlayerFlags,
	// If,
	[Description("ifgotweaponce")]
	IfGotWeaponOnce,
	[Description("ifp")]
	IfP,
	[Description("ifpdistg")]
	IfPDistG,
	[Description("ifpdistl")]
	IfPDistL,
	[Description("ifphealthl")]
	IfPHealthL,
	[Description("ifpinventory")]
	IfPInventory,
	[Description("ifplayersl")]
	IfPlayersL,
	// Sectors,
	// Operating,
	[Description("operate")]
	Operate,
	[Description("operateactivators")]
	OperateActivators,
	[Description("operatemasterswitches")]
	OperateMasterSwitches,
	[Description("operaterespawns")]
	OperateRespawns,
	[Description("operatesectors")]
	OperateSectors,
	[Description("activatebysector")]
	ActivateBySector,
	[Description("activate")]
	Activate,

	// Manipulation,
	[Description("dragpoint")]
	DragPoint,
	[Description("movesector")]
	MoveSector,
	[Description("sectsetinterpolation")]
	SectSetInterpolation,
	[Description("sectclearinterpolation")]
	SectClearInterpolation,
	// Analysis,
	[Description("getceilzofslope")]
	GetCeilZOfSlope,
	[Description("getflorzofslope")]
	GetFlorZOfSlope,
	[Description("getzrange")]
	GetZRange,
	[Description("updatesector")]
	UpdateSector,
	[Description("updatesectorz")]
	UpdateSectorZ,
	[Description("checkactivatormotion")]
	CheckActivatorMotion,
	[Description("rotatepoint")]
	RotatePoint,
	[Description("lineintersect")]
	LineIntersect,
	[Description("rayintersect")]
	RayIntersect,
	[Description("sectorofwall")]
	SectorOfWall,

	// Discovery,
	// Searching,
	[Description("findnearactor")]
	FindNearActor,
	[Description("findnearactor3d")]
	FindNearActor3d,
	[Description("findnearactor3dvar")]
	FindNearActor3dVar,
	[Description("findnearactorvar")]
	FindNearActorVar,
	[Description("findnearactorz")]
	FindNearActorZ,
	[Description("findnearactorzvar")]
	FindNearActorZVar,
	[Description("findnearsprite")]
	FindNearSprite,
	[Description("findnearsprite3d")]
	FindNearSprite3d,
	[Description("findnearsprite3dvar")]
	FindNearSprite3dVar,
	[Description("findnearspritevar")]
	FindNearSpriteVar,
	[Description("findnearspritez")]
	FindNearSpriteZ,
	[Description("findnearspritezvar")]
	FindNearSpriteZVar,
	[Description("findotherplayer")]
	FindOtherPlayer,
	[Description("findplayer")]
	FindPlayer,
	[Description("neartag")]
	NearTag,
	[Description("hitscan")]
	Hitscan,

	// Sorting,
	[Description("headspritesect")]
	HeadSpriteSect,
	[Description("headspritestat")]
	HeadSpriteStat,
	[Description("nextspritesect")]
	NextSpriteSect,
	[Description("nextspritestat")]
	NextSpriteStat,
	[Description("prevspritesect")]
	PrevSpriteSect,
	[Description("prevspritestat")]
	PrevSpriteStat,

	// Audio,
	// Sounds,
	[Description("definesound")]
	DefineSound,
	[Description("sound")]
	Sound,
	[Description("soundvar")]
	SoundVar,
	[Description("soundonce")]
	SoundOnce,
	[Description("soundoncevar")]
	SoundOnceVar,
	[Description("globalsound")]
	GlobalSound,
	[Description("globalsoundvar")]
	GlobalSoundVar,
	[Description("screensound")]
	ScreenSound,
	[Description("stopsound")]
	StopSound,
	[Description("stopsoundvar")]
	StopSoundVar,
	[Description("stopactorsound")]
	StopActorSound,
	[Description("stopallsounds")]
	StopAllSounds,
	[Description("ifsound")]
	IfSound,
	[Description("ifactorsound")]
	IfActorSound,
	[Description("ifnosounds")]
	IfNoSounds,
	[Description("setactorsoundpitch")]
	SetActorSoundPitch,

	// Music,
	[Description("music")]
	Music,
	[Description("starttrack")]
	StartTrack,
	[Description("starttrackvar")]
	StartTrackVar,
	[Description("getmusicposition")]
	GetMusicPosition,
	[Description("setmusicposition")]
	SetMusicPosition,

	// Quotes,
	[Description("definequote")]
	DefineQuote,
	[Description("redefinequote")]
	RedefineQuote,
	[Description("quote")]
	Quote,
	[Description("userquote")]
	UserQuote,
	[Description("qsprintf")]
	QSprintf,
	[Description("qstrcpy")]
	QStrCpy,
	[Description("qstrcat")]
	QStrCat,
	[Description("qstrncat")]
	QStrNCat,
	[Description("qstrlen")]
	QStrLen,
	[Description("qsubstr")]
	QSubStr,
	[Description("qstrdim")]
	QStrDim,
	[Description("qgetsysstr")]
	QGetSysStr,
	[Description("getpname")]
	GetPName,
	[Description("getkeyname")]
	GetKeyName,

	// Cutscenes,
	[Description("startcutscene")]
	StartCutscene,
	[Description("ifcutscene")]
	IfCutscene,
	[Description("Screen")]
	Screen,

	// Screen Manipulation,
	[Description("palfrom")]
	PalFrom,
	[Description("guniqhudid")]
	GUniqHudId,
	[Description("setgamepalette")]
	SetGamePalette,
	[Description("setaspect")]
	SetAspect,

	// Player Actions,
	[Description("wackplayer")]
	WackPlayer,
	[Description("quake")]
	Quake,
	[Description("pkick")]
	PKick,
	[Description("pstomp")]
	PStomp,
	[Description("tip")]
	Tip,

	// Screen Drawing,
	[Description("rotatesprite")]
	RotateSprite,
	[Description("rotatesprite16")]
	RotateSprite16,
	[Description("rotatespritea")]
	RotateSpriteA,
	[Description("screentext")]
	ScreenText,
	[Description("gametext")]
	GameText,
	[Description("gametextz")]
	GameTextZ,
	[Description("minitext")]
	MiniText,
	[Description("digitalnumber")]
	DigitalNumber,
	[Description("digitalnumberz")]
	DigitalNumberZ,
	[Description("showview")]
	ShowView,
	[Description("showviewunbiased")]
	ShowViewUnbiased,

	// Math,
	[Description("displayrand")]
	DisplayRand,
	[Description("displayrandvar")]
	DisplayRandVar,
	[Description("displayrandvarvar")]
	DisplayRandVarVar,

	// Time Access,
	[Description("getticks")]
	GetTicks,
	[Description("gettimedate")]
	GetTimeDate,

	// Game-Changing,
	[Description("activatecheat")]
	ActivateCheat,
	[Description("startlevel")]
	StartLevel,
	[Description("inittimer")]
	InitTimer,
	[Description("endofgame")]
	EndOfGame,
	[Description("endoflevel")]
	EndOfLevel,
	[Description("cmenu")]
	CMenu,

	// Game Saving,
	[Description("save")]
	Save,
	[Description("savenn")]
	SaveNn,

	// Hub Maps,
	[Description("loadmapstate")]
	LoadMapState,
	[Description("savemapstate")]
	SaveMapState,
	[Description("clearmapstate")]
	ClearMapState,

	// Debug,
	[Description("debug")]
	Debug,
	[Description("addlog")]
	AddLog,
	[Description("addlogvar")]
	AddLogVar,
	[Description("echo")]
	Echo,

	// Deprecated,
	[Description("betaname")]
	BetaName,
	[Description("enhanced")]
	Enhanced,
	[Description("eventloadactor")]
	EventLoadActor,
	[Description("time")]
	Time,
	[Description("shadeto")]
	ShadeTo,

	// Screen Drawing,
	[Description("myos")]
	Myos,
	[Description("myosx")]
	MyosX,
	[Description("myospal")]
	MyosPal,
	[Description("myospalx")]
	MyosPalX,

	// Single-Use Structure Access,
	[Description("getactorangle")]
	GetActorAngle,
	[Description("getplayerangle")]
	GetPlayerAngle,
	[Description("gettextureceiling")]
	GetTextureCeiling,
	[Description("gettexturefloor")]
	GetTextureFloor,
	[Description("sectgethitag")]
	SectGetHitag,
	[Description("sectgetlotag")]
	SectGetLotag,
	[Description("spgethitag")]
	SpGetHitag,
	[Description("spgetlotag")]
	SpGetLotag,
	[Description("setactorangle")]
	SetActorAngle,
	[Description("setplayerangle")]
	SetPlayerAngle
}
