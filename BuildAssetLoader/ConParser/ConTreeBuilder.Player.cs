namespace BuildAssetLoader.Con
{
    // Flat commands from Commands.Player.cs (Player - Commands).
    // AI Assisted
    public static partial class ConTreeBuilder
    {
        private static Command? TryParsePlayer(CommandList command, ConTreeCursor cursor) => command switch
        {
            CommandList.AddAmmo => new AddAmmoCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.AddInventory => new AddInventoryCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.AddWeapon => new AddWeaponCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.AddWeaponVar => new AddWeaponVarCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.AddPHealth => new AddPHealthCommand(cursor.ReadValue()),
            CommandList.TossWeapon => new TossWeaponCommand(),
            CommandList.GMaxAmmo => new GMaxAmmoCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.SMaxAmmo => new SMaxAmmoCommand(cursor.ReadValue(), cursor.ReadValue()),
            CommandList.CheckAvailInven => new CheckAvailInvenCommand(cursor.ReadValue()),
            CommandList.CheckAvailWeapon => new CheckAvailWeaponCommand(cursor.ReadValue()),
            CommandList.AddKills => new AddKillsCommand(cursor.ReadValue()),
            CommandList.LockPlayer => new LockPlayerCommand(cursor.ReadValue()),
            CommandList.ResetPlayer => new ResetPlayerCommand(),
            CommandList.ResetPlayerFlags => new ResetPlayerFlagsCommand(cursor.ReadInt()),

            _ => null,
        };
    }
}
