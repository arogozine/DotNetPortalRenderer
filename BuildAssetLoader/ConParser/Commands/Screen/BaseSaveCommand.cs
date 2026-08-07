namespace BuildAssetLoader.Con
{
    // ===== Game Saving =====

    // save/savenn <slot number> — creates a savegame in slot 0-9; savenn keeps an existing name if present.
    public record BaseSaveCommand(CommandList Start, string SlotNumber) : Command(Start);
}

