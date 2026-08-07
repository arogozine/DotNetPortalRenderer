using BuildAssetLoader.Con;

namespace Tests;

public class ConTreeBuilderTests
{
    [SkipIfDirectoryNotSetTheory(TestDataDirectory.DukeNukem)]
    [InlineData("DEFS.CON")]
    [InlineData("USER.CON")]
    [InlineData("GAME.CON")]
    public void Build_RealVanillaConFile_ParsesWithoutThrowing(string fileName)
    {
        string text = File.ReadAllText(Path.Combine(TestConfiguration.Current.DukeNukemDirectory, fileName));

        List<ConToken> tokens = ConParser.Parse(text);
        List<Command> commands = ConTreeBuilder.Build(tokens);

        Assert.NotEmpty(commands);
    }

    [Fact]
    public void Build_IfElseWithoutBraces_AttachesSingleStatementBranches()
    {
        List<ConToken> tokens = ConParser.Parse("""
            useractor enemy MYENEMY
              ifspritepal 0
                strength MYENEMY_NORMAL_STRENGTH
              else
                strength MYENEMY_TOUGHER_STRENGTH
            enda
            """);

        UserActorCommand actor = Assert.IsType<UserActorCommand>(Assert.Single(ConTreeBuilder.Build(tokens)));
        IfspritepalCommand condition = Assert.IsType<IfspritepalCommand>(Assert.Single(actor.Body));

        StrengthCommand ifBranch = Assert.IsType<StrengthCommand>(Assert.Single(condition.Body));
        Assert.Equal("MYENEMY_NORMAL_STRENGTH", ifBranch.Number);

        Assert.NotNull(condition.ElseBody);
        StrengthCommand elseBranch = Assert.IsType<StrengthCommand>(Assert.Single(condition.ElseBody));
        Assert.Equal("MYENEMY_TOUGHER_STRENGTH", elseBranch.Number);
    }

    [Fact]
    public void Build_MoveDeclareThenInvoke_IsDisambiguatedByContext()
    {
        List<ConToken> tokens = ConParser.Parse("""
            move MYSPEED 64
            useractor enemy MYENEMY
              move MYSPEED randomangle
            enda
            """);

        List<Command> commands = ConTreeBuilder.Build(tokens);
        MoveCommand declaration = Assert.IsType<MoveCommand>(commands[0]);
        Assert.Equal("MYSPEED", declaration.Name);
        Assert.Equal(64, declaration.Horizontal);
        Assert.Null(declaration.Vertical);

        UserActorCommand actor = Assert.IsType<UserActorCommand>(commands[1]);
        MoveInvokeCommand invocation = Assert.IsType<MoveInvokeCommand>(Assert.Single(actor.Body));
        Assert.Equal("MYSPEED", invocation.Name);
        Assert.NotNull(invocation.MoveFlag);
        Assert.Equal(["randomangle"], invocation.MoveFlag);
    }
}
