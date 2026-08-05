namespace BuildAssetLoader.Con
{
    // Flat commands from Commands.Player.cs (Player - Commands).
    // AI Assisted
    public static partial class ConTreeBuilder
    {
        private static Command? TryParsePlayer(CommandList command, ConTreeCursor cursor) => command switch
        {
            CommandList.addammo => new AddammoCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.addinventory => new AddinventoryCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.addweapon => new AddweaponCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.addweaponvar => new AddweaponvarCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.addphealth => new AddphealthCommand(cursor.ReadValue()),
            CommandList.tossweapon => new TossweaponCommand(),
            CommandList.gmaxammo => new GmaxammoCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.smaxammo => new SmaxammoCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.checkavailinven => new CheckavailinvenCommand(cursor.ReadValue()),
            CommandList.checkavailweapon => new CheckavailweaponCommand(cursor.ReadValue()),
            CommandList.addkills => new AddkillsCommand(cursor.ReadValue()),
            CommandList.lockplayer => new LockplayerCommand(cursor.ReadValue()),
            CommandList.resetplayer => new ResetplayerCommand(),
            CommandList.resetplayerflags => new ResetplayerflagsCommand(cursor.ReadInt()),

            _ => null,
        };
    }
}
