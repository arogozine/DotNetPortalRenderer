// See: https://wiki.eduke32.com/wiki/Category:All_commands

namespace BuildAssetLoader.Con
{
    /// <summary>Shared base for the <c>sound</c>/<c>soundvar</c> family of commands (including <c>soundonce</c>,
    /// <c>globalsound</c>, <c>stopsound</c> and their <c>var</c> variants), all of which play or reference a sound
    /// defined by <c>definesound</c> given a single sound number/name.</summary>
    public record BaseSoundCommand(CommandList Start, string SoundNumber) : Command(Start);
}
