// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Deprecated form of <c>useractor</c>: opens an actor code block for tile <c>PicNum</c>, always using
    /// the "notenemy" hardcoded behavior type. <c>Stength</c>, <c>Action</c>, <c>Move</c> and <c>MoveFlag</c> set the
    /// actor's initial health, action, move and hardcoded movement flags, defaulting to 0/none when omitted. The
    /// block runs until the matching <c>enda</c>.</summary>
    [Description("actor")]
    public record ActorCommand(
        string PicNum,
        string? Stength,
        string? Action,
        string? Move,
        string[]? MoveFlag)
        : BaseActorCommand(CommandList.Actor, PicNum, Stength, Action, Move, MoveFlag);
}
