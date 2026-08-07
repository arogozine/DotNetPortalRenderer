namespace BuildAssetLoader.Con
{
    // cheatkeys <scan code> <scan code>
    public sealed record CheatkeysCommand(int ScanCode1, int ScanCode2) : Command(CommandList.cheatkeys);
}

