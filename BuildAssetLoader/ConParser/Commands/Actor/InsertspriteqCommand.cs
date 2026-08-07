// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Inserts the current actor's sprite into the decal deletion queue. The sprite's statnum must allow
    /// <c>killit</c> to actually delete queued sprites, or unexpected behavior can result.</summary>
    [Description("insertspriteq")]
    public sealed record InsertSpriteQCommand() : Command(CommandList.InsertSpriteQ);
}
