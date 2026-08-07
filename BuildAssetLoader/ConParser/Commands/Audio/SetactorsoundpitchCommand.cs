// See: https://wiki.eduke32.com/wiki/Category:All_commands
using System.ComponentModel;

namespace BuildAssetLoader.Con
{
    /// <summary>Changes the pitch of a sound currently playing from actor <c>ActorId</c>. <c>PitchOffset</c> uses
    /// the same units as the <c>definesound</c> pitch range endpoints (1/100th of a semitone); as with the random
    /// pitch offset, raising pitch shortens the sound's duration and lowering it lengthens it.</summary>
    [Description("setactorsoundpitch")]
    public sealed record SetActorSoundPitchCommand(
        string ActorId,
        string Sound,
        string PitchOffset) : Command(CommandList.SetActorSoundPitch);
}
