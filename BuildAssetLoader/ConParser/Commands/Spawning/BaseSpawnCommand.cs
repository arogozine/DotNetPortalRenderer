namespace BuildAssetLoader.Con
{
    // ===== Spawning =====
    // spawn/espawn/qspawn/eqspawn place <tile number> at the spawning actor's position.
    // "e" prefix sets gamevar RETURN to the new sprite's id; "q" prefix inserts it into the decal deletion queue;
    // "var" suffix takes a gamevar rather than a constant/define for <tile number>.

    public record BaseSpawnCommand(CommandList Start, string TileNumber) : Command(Start);
}

