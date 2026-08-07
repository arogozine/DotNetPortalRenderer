namespace BuildAssetLoader.Con
{
    // gamestartup <param1> <param2> ... <paramN> — 26 (v1.3D) or 30 (v1.5) startup parameters.
    public sealed record GamestartupCommand(string[] Parameters) : Command(CommandList.gamestartup);
}

