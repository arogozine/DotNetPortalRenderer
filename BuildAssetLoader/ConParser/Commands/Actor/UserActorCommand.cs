// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Opens an actor code block for tile <c>PicNum</c>, declaring its hardcoded behavior <c>Type</c>
    /// (e.g. "notenemy", "enemy", "enemystayput"). <c>Stength</c>, <c>Action</c>, <c>Move</c> and <c>MoveFlag</c> set
    /// the actor's initial health, action, move and hardcoded movement flags, defaulting to 0/none when omitted. The
    /// block runs until the matching <c>enda</c>.</summary>
    [Description("useractor")]
    public record UserActorCommand(
        string Type,
        string PicNum,
        string? Stength,
        string? Action,
        string? Move,
        string[]? MoveFlag)
        : BaseActorCommand(CommandList.UserActor, PicNum, Stength, Action, Move, MoveFlag);
}
