// See: https://wiki.eduke32.com/wiki/Category:All_commands
namespace BuildAssetLoader.Con
{
    /// <summary>The comparison performed by an <c>ifvar</c>/<c>ifvarvar</c> family command (e.g. <c>ifvare</c>,
    /// <c>ifvarvarg</c>) between a gamevar and its second operand. <see cref="IfVarCommand"/> and
    /// <see cref="IfVarVarCommand"/> carry one of these values, resolved from the originating CON token via
    /// <see cref="GamevarConditionLookup"/>.</summary>
    public enum GamevarCondition { Equal, NotEqual, Greater, Less, And, Or, Xor, Either }
}

