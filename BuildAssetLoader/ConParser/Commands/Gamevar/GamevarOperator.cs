// See: https://wiki.eduke32.com/wiki/Category:All_commands
namespace BuildAssetLoader.Con
{
    // ===== Gamevar operator / condition enums =====

    /// <summary>The binary operation performed by a <c>*var</c>/<c>*varvar</c> family command (e.g. <c>setvar</c>,
    /// <c>addvarvar</c>) that assigns to a gamevar. <see cref="VarOpCommand"/> and <see cref="VarVarOpCommand"/>
    /// carry one of these values, resolved from the originating CON token via
    /// <see cref="GamevarOperatorLookup"/>.</summary>
    public enum GamevarOperator { Set, Add, Sub, Mul, Div, Mod, And, Or, Xor, Rand }
}

