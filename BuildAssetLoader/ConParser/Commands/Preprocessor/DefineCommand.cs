// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    // ===== Preprocessor =====

    /// <summary>Associates a plaintext label with a value, so the label can be used anywhere the value would otherwise
    /// appear. <c>Value</c> is usually an int literal, but may also be another define's name or a symbolic constant
    /// (e.g. YES/NO), so it isn't resolved/parsed as an int here.</summary>
    [Description("define")]
    public record DefineCommand(
        string Name,
        string Value) : Command(CommandList.Define);
}
