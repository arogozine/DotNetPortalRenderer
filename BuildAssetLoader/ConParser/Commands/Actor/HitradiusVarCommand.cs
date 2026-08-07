namespace BuildAssetLoader.Con
{
    // hitradiusvar <radius> <1> <2> <3> <4> — gamevar-driven radius damage.
    public sealed record HitradiusVarCommand(string Radius, string Damage1, string Damage2, string Damage3, string Damage4)
        : Command(CommandList.hitradiusvar);
}

