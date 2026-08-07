// See: https://wiki.eduke32.com/wiki/Category:All_commands
namespace BuildAssetLoader.Con
{
    /// <summary>One <c>case &lt;constant&gt;</c> (or <c>default</c>) block inside a <see cref="SwitchCommand"/>.
    /// Not a standalone <see cref="Command"/> — CommandList.Case/CommandList.Default are structural markers,
    /// consumed here rather than modeled as their own Command types.</summary>
    public sealed record CaseBlock(int? Constant, bool IsDefault, List<Command> Body);
}
