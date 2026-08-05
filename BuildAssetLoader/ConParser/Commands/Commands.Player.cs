namespace BuildAssetLoader.Con
{
    // ===== Player - Commands =====

    // addammo <weapon> <amount> — gives ammo without giving the weapon itself. <amount> is frequently a define.
    public sealed record AddammoCommand(string Weapon, string Amount) : Command(CommandList.addammo);

    // addinventory <index> <amount> — sets an inventory item's amount (or adjusts armor/gives an access card).
    // <amount> is frequently a define (e.g. STEROID_AMOUNT).
    public sealed record AddinventoryCommand(string Index, string Amount) : Command(CommandList.addinventory);

    // addweapon/addweaponvar <weapon> <amount> — gives the weapon with ammo to the nearest player.
    public record BaseAddweaponCommand(CommandList Start, string Weapon, string Amount) : Command(Start);

    public sealed record AddweaponCommand(string Weapon, string Amount) : BaseAddweaponCommand(CommandList.addweapon, Weapon, Amount);
    public sealed record AddweaponvarCommand(string Weapon, string Amount) : BaseAddweaponCommand(CommandList.addweaponvar, Weapon, Amount);

    // addphealth <amount> — adds (or subtracts) player health. Commonly a define (e.g. SHARKBITESTRENGTH).
    public sealed record AddphealthCommand(string Amount) : Command(CommandList.addphealth);

    // tossweapon — spawns the currently selected weapon on player death.
    public sealed record TossweaponCommand() : Command(CommandList.tossweapon);

    // gmaxammo <WeaponID> <return> — gets the global max ammo for a weapon.
    public sealed record GmaxammoCommand(string WeaponId, string ReturnVar) : Command(CommandList.gmaxammo);

    // smaxammo <WeaponID> <maxamount> — sets the global max ammo for a weapon.
    public sealed record SmaxammoCommand(string WeaponId, string MaxAmount) : Command(CommandList.smaxammo);

    // checkavailinven <playerID> — selects the first available inventory item, pruning empties.
    public sealed record CheckavailinvenCommand(string PlayerId) : Command(CommandList.checkavailinven);

    // checkavailweapon <playerID> — selects the best available weapon per wchoice.
    public sealed record CheckavailweaponCommand(string PlayerId) : Command(CommandList.checkavailweapon);

    // addkills <number> — adds to the closest player's kill score; also clears the current actor's stayput flag.
    public sealed record AddkillsCommand(string Number) : Command(CommandList.addkills);

    // lockplayer <gamevar> — freezes player movement for <gamevar> tics.
    public sealed record LockplayerCommand(string Gamevar) : Command(CommandList.lockplayer);

    // resetplayer — reloads the map (single player) and clears the player's inventory.
    public sealed record ResetplayerCommand() : Command(CommandList.resetplayer);

    // resetplayerflags <flags> — same as resetplayer, with a bitfield of extra options.
    public sealed record ResetplayerflagsCommand(int Flags) : Command(CommandList.resetplayerflags);
}
