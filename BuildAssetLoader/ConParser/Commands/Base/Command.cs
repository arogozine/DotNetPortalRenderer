// See: https://wiki.eduke32.com/wiki/Category:All_commands
namespace BuildAssetLoader.Con
{
    // ===== Base shapes =====

    /// <summary>Base type for every parsed CON statement. <see cref="StartToken"/> identifies which CON keyword
    /// produced this node.</summary>
    public abstract record Command(CommandList StartToken);
}
