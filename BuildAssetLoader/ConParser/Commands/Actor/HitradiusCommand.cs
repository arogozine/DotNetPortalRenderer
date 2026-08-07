namespace BuildAssetLoader.Con
{
    // ===== Surroundings - Commands =====

    // hitradius <radius> <1> <2> <3> <4> — constant/define-driven radius damage (e.g. WEAKEST, WEAK, MEDIUMSTRENGTH, TOUGH).
    public sealed record HitradiusCommand(string Radius, string Damage1, string Damage2, string Damage3, string Damage4)
        : Command(CommandList.hitradius);
}

