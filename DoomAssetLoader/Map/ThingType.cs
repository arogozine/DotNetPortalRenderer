using System.ComponentModel;

namespace DoomAssetLoader.Map
{
    public enum ThingType : short
    {
        [Description("PLAY")]
        Player1Start = 1,
        [Description("PLAY")]
        Player2Start = 2,
        [Description("PLAY")]
        Player3Start = 3,
        [Description("PLAY")]
        Player4Start = 4,
        [Description("BKEY")]
        BlueKeycard = 5,
        [Description("YKEY")]
        YellowKeycard = 6,
        [Description("SPID")]
        Spiderdemon = 7,
        [Description("BPAK")]
        Backpack = 8,
        [Description("SPOS")]
        ShotgunGuy = 9,
        [Description("PLAY")]
        BloodyMess1 = 10,
        [Description("NONE")]
        DeathmatchStart = 11,
        [Description("PLAY")]
        BloodyMess2 = 12,
        [Description("RKEY")]
        RedKeycard = 13,
        [Description("NONE1")]
        TeleportLanding = 14,
        [Description("PLAY")]
        DeadPlayer = 15,
        [Description("CYBR")]
        Cyberdemon = 16,
        [Description("CELP")]
        EnergyCellPack = 17,
        [Description("POSS")]
        DeadFormerHuman = 18,
        [Description("SPOS")]
        DeadFormerSergeant = 19,
        [Description("TROO")]
        DeadImp = 20,
        [Description("SARG")]
        DeadDemon = 21,
        [Description("HEAD")]
        DeadCacodemon = 22,
        [Description("SKUL")]
        DeadLostSoul = 23,
        [Description("POL5")]
        PoolOfBloodAndFlesh = 24,
        [Description("POL1")]
        ImpaledHuman = 25,
        [Description("POL6")]
        TwitchingImpaledHuman = 26,
        [Description("POL4")]
        SkullOnPole = 27,
        [Description("POL2")]
        FiveSkulls = 28,
        [Description("POL3")]
        PileOfSkullsAndCandles = 29,
        [Description("COL1")]
        TallGreenPillar = 30,
        [Description("COL2")]
        ShortGreenPillar = 31,
        [Description("COL3")]
        TallRedPillar = 32,
        [Description("COL4")]
        ShortRedPillar = 33,
        [Description("CAND")]
        Candle = 34,
        [Description("CBRA")]
        Candelabra = 35,
        [Description("COL5")]
        ShortGreenPillarWithHeart = 36,
        [Description("COL6")]
        ShortRedPillarWithSkull = 37,
        [Description("RSKU")]
        RedSkullKey = 38,
        [Description("YSKU")]
        YellowSkullKey = 39,
        [Description("BSKU")]
        BlueSkullKey = 40,
        [Description("CEYE")]
        EvilEye = 41,
        [Description("FSKU")]
        FloatingSkull = 42,
        [Description("TRE1")]
        BurntTree = 43,
        [Description("TBLU")]
        TallBlueFirestick = 44,
        [Description("TGRN")]
        TallGreenFirestick = 45,
        [Description("TRED")]
        TallRedFirestick = 46,
        [Description("SMIT")]
        BrownStump = 47,
        [Description("ELEC")]
        TallTechnoColumn = 48,
        [Description("GOR1")]
        HangingVictimTwitching = 49,
        [Description("GOR2")]
        HangingVictimArmsOut = 50,
        [Description("GOR3")]
        HangingVictimOneLegged = 51,
        [Description("GOR4")]
        HangingPairOfLegs = 52,
        [Description("GOR5")]
        HangingLeg = 53,
        [Description("TRE2")]
        LargeBrownTree = 54,
        [Description("SMBT")]
        ShortBlueFirestick = 55,
        [Description("SMGT")]
        ShortGreenFirestick = 56,
        [Description("SMRT")]
        ShortRedFirestick = 57,
        [Description("SARG")]
        Spectre = 58,
        [Description("GOR2")]
        HangingVictimArmsOut2 = 59,
        [Description("GOR4")]
        HangingPairOfLegs2 = 60,
        [Description("GOR3")]
        HangingVictimOneLegged2 = 61,
        [Description("GOR5")]
        HangingLeg2 = 62,
        [Description("GOR1")]
        HangingVictimTwitching2 = 63,
        [Description("VILE")]
        ArchVile = 64,
        [Description("CPOS")]
        HeavyWeaponDude = 65,
        [Description("SKEL")]
        Revenant = 66,
        [Description("FATT")]
        Mancubus = 67,
        [Description("BSPI")]
        Arachnotron = 68,
        [Description("BOS2")]
        HellKnight = 69,
        [Description("FCAN")]
        BurningBarrel = 70,
        [Description("PAIN")]
        PainElemental = 71,
        [Description("KEEN")]
        CommanderKeen = 72,
        [Description("HDB1")]
        HangingVictimGutsRemoved = 73,
        [Description("HDB2")]
        HangingVictimGutsAndBrainRemoved = 74,
        [Description("HDB3")]
        HangingTorsoLookingDown = 75,
        [Description("HDB4")]
        HangingTorsoOpenSkull = 76,
        [Description("HDB5")]
        HangingTorsoLookingUp = 77,
        [Description("HDB6")]
        HangingTorsoBrainRemoved = 78,
        [Description("POB1")]
        PoolOfBlood1 = 79,
        [Description("POB2")]
        PoolOfBlood2 = 80,
        [Description("BRS1")]
        PoolOfBrains = 81,
        [Description("SGN2")]
        SuperShotgun = 82,
        [Description("MEGA")]
        Megasphere = 83,
        [Description("SSWV")]
        WolfensteinSS = 84,
        [Description("TLMP")]
        TallTechnoFloorLamp = 85,
        [Description("TLP2")]
        ShortTechnoFloorLamp = 86,
        [Description("NONE4")]
        SpawnSpot = 87,
        [Description("BBRN")]
        RomerosHead = 88,
        [Description("NONE6")]
        MonsterSpawner = 89,
        [Description("SHOT")]
        ShotgunPickup = 2001,
        [Description("MGUN")]
        ChaingunPickup = 2002,
        [Description("LAUN")]
        RocketLauncherPickup = 2003,
        [Description("PLAS")]
        PlasmaGunPickup = 2004,
        [Description("CSAW")]
        ChainsawPickup = 2005,
        [Description("BFUG")]
        BFG9000Pickup = 2006,
        [Description("CLIP")]
        ClipPickup = 2007,
        [Description("SHEL")]
        FourShotgunShells = 2008,
        [Description("ROCK")]
        RocketPickup = 2010,
        [Description("STIM")]
        Stimpack = 2011,
        [Description("MEDI")]
        Medikit = 2012,
        [Description("SOUL")]
        Supercharge = 2013,
        [Description("BON1")]
        HealthBonus = 2014,
        [Description("BON2")]
        ArmorBonus = 2015,
        [Description("ARM1")]
        ArmorGreen = 2018,
        [Description("ARM2")]
        MegaArmor = 2019,
        [Description("PINV")]
        Invulnerability = 2022,
        [Description("PSTR")]
        Berserk = 2023,
        [Description("PINS")]
        PartialInvisibility = 2024,
        [Description("SUIT")]
        RadiationSuit = 2025,
        [Description("PMAP")]
        ComputerMap = 2026,
        [Description("COLU")]
        FloorLamp = 2028,
        [Description("BAR1")]
        ExplodingBarrel = 2035,
        [Description("PVIS")]
        LightAmplificationVisor = 2045,
        [Description("BROK")]
        BoxOfRockets = 2046,
        [Description("CELL")]
        EnergyCell = 2047,
        [Description("AMMO")]
        BoxOfBullets = 2048,
        [Description("SBOX")]
        BoxOfShotgunShells = 2049,

        [Description("TROO")]
        Imp = 3001,
        [Description("SARG")]
        Demon = 3002,
        [Description("BOSS")]
        BaronOfHell = 3003,
        [Description("POSS")]
        Zombieman = 3004,
        [Description("HEAD")]
        Cacodemon = 3005,
        [Description("SKUL")]
        LostSoul = 3006
    }
}
