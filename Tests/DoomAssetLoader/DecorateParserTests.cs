// AI Assisted
using DoomAssetLoader.Decorate;

namespace Tests;

public class DecorateParserTests
{
    [Fact]
    public void Parse_ZombieManWorkedExample_ParsesHeaderAndAllStateLabels()
    {
        const string decorate = """
            actor ZombieMan 3004
            {
                States
                {
                Spawn:
                    POSS AB 10 A_Look
                See:
                    POSS AABBCCDD 4 A_Chase
                Missile:
                    POSS E 10 A_FaceTarget
                    POSS F 8 A_PosAttack
                    POSS E 8
                    goto See
                Pain:
                    POSS G 3
                    POSS G 3 A_Pain
                    goto See
                Death:
                    POSS H 5
                    POSS I 5 A_Scream
                    POSS J 5 A_Fall
                    POSS K 5
                    POSS L -1
                    stop
                XDeath:
                    POSS M 5
                    POSS N 5 A_XScream
                    POSS O 5 A_Fall
                    POSS PQRST 5
                    POSS U -1
                    stop
                Raise:
                    POSS KJIH 5
                    goto See
                }
            }
            """;

        List<DecorateActor> actors = DecorateParser.Parse(decorate);

        DecorateActor actor = Assert.Single(actors);
        Assert.Equal("ZombieMan", actor.Name);
        Assert.Null(actor.Parent);
        Assert.Equal(3004, actor.DoomEdNum);

        Assert.Contains(actor.States, static entry => entry is DecorateStateLabel { Name: "Spawn" });
        Assert.Contains(actor.States, static entry => entry is DecorateStateLabel { Name: "XDeath" });
        Assert.Contains(actor.States, static entry => entry is DecorateFlowControl { Kind: DecorateFlowControlKind.Stop });
    }

    [Fact]
    public void Parse_ActorWithParentAndReplaces_ParsesInheritanceHeader()
    {
        const string decorate = """
            actor SuperImp : DoomImp replaces DoomImp 3001
            {
                States
                {
                Raise:
                    stop
                }
            }
            """;

        DecorateActor actor = Assert.Single(DecorateParser.Parse(decorate));

        Assert.Equal("SuperImp", actor.Name);
        Assert.Equal("DoomImp", actor.Parent);
        Assert.Equal("DoomImp", actor.Replaces);
        Assert.Equal(3001, actor.DoomEdNum);

        DecorateStateLabel label = Assert.IsType<DecorateStateLabel>(actor.States[0]);
        Assert.Equal("Raise", label.Name);
        DecorateFlowControl flow = Assert.IsType<DecorateFlowControl>(actor.States[1]);
        Assert.Equal(DecorateFlowControlKind.Stop, flow.Kind);
    }

    [Fact]
    public void Parse_PlusAndMinusFlags_RecordsFlagValues()
    {
        const string decorate = """
            actor FlagTest
            {
                +SOLID
                -SHOOTABLE
                +Inventory.INVBAR
            }
            """;

        DecorateActor actor = Assert.Single(DecorateParser.Parse(decorate));

        Assert.True(actor.Flags["SOLID"]);
        Assert.False(actor.Flags["SHOOTABLE"]);
        Assert.True(actor.Flags["Inventory.INVBAR"]);
    }

    [Fact]
    public void Parse_SingleValueProperty_CapturesOneArgument()
    {
        const string decorate = """
            actor PropTest
            {
                Health 20
            }
            """;

        DecorateActor actor = Assert.Single(DecorateParser.Parse(decorate));

        DecorateProperty health = actor.Properties["Health"];
        Assert.Equal(["20"], health.Arguments);
    }

    [Fact]
    public void Parse_MultiValueProperty_SplitsArgumentsOnCommas()
    {
        const string decorate = """
            actor PropTest
            {
                DropItem "Clip", 255, 1
            }
            """;

        DecorateActor actor = Assert.Single(DecorateParser.Parse(decorate));

        DecorateProperty dropItem = actor.Properties["DropItem"];
        Assert.Equal(["Clip", "255", "1"], dropItem.Arguments);
    }

    [Fact]
    public void Parse_ParenthesizedExpressionProperty_CapturesRawExpressionText()
    {
        const string decorate = """
            actor PropTest
            {
                Damage (3*8)
            }
            """;

        DecorateActor actor = Assert.Single(DecorateParser.Parse(decorate));

        DecorateProperty damage = actor.Properties["Damage"];
        Assert.Equal(["(3*8)"], damage.Arguments);
    }

    [Fact]
    public void Parse_BareKeywordProperty_HasNoArguments()
    {
        const string decorate = """
            actor PropTest
            {
                Monster
                Health 10
            }
            """;

        DecorateActor actor = Assert.Single(DecorateParser.Parse(decorate));

        Assert.Empty(actor.Properties["Monster"].Arguments);
        Assert.Equal(["10"], actor.Properties["Health"].Arguments);
    }

    [Fact]
    public void Parse_DottedStateLabel_ParsesAsSingleLabelName()
    {
        const string decorate = """
            actor DottedLabelTest
            {
                States
                {
                Death.Extreme:
                    TNT1 A 0
                    stop
                }
            }
            """;

        DecorateActor actor = Assert.Single(DecorateParser.Parse(decorate));

        DecorateStateLabel label = Assert.IsType<DecorateStateLabel>(actor.States[0]);
        Assert.Equal("Death.Extreme", label.Name);
    }

    [Fact]
    public void Parse_FrameCollapsingStateLine_KeepsFrameLettersTogether()
    {
        const string decorate = """
            actor FrameTest
            {
                States
                {
                See:
                    POSS AABBCCDD 4 A_Chase
                }
            }
            """;

        DecorateActor actor = Assert.Single(DecorateParser.Parse(decorate));

        DecorateStateDefinition state = Assert.IsType<DecorateStateDefinition>(actor.States[1]);
        Assert.Equal("POSS", state.Sprite);
        Assert.Equal("AABBCCDD", state.Frames);
        Assert.Equal("4", state.Duration);
        Assert.Equal("A_Chase", state.ActionFunction);
    }

    [Fact]
    public void Parse_StateWithBrightAndOffsetKeywords_ParsesKeywordsAndAction()
    {
        const string decorate = """
            actor KeywordTest
            {
                States
                {
                Spawn:
                    STUF A 5 Bright Offset(2,-3) A_Look
                }
            }
            """;

        DecorateActor actor = Assert.Single(DecorateParser.Parse(decorate));

        DecorateStateDefinition state = Assert.IsType<DecorateStateDefinition>(actor.States[1]);
        Assert.True(state.Bright);
        Assert.Equal((2, -3), state.Offset);
        Assert.Equal("A_Look", state.ActionFunction);
    }

    [Fact]
    public void Parse_GotoWithOffset_ParsesLabelAndOffset()
    {
        const string decorate = """
            actor GotoTest
            {
                States
                {
                Spawn:
                    TNT1 A 0
                    goto Spawn+1
                }
            }
            """;

        DecorateActor actor = Assert.Single(DecorateParser.Parse(decorate));

        DecorateFlowControl flow = Assert.IsType<DecorateFlowControl>(actor.States[2]);
        Assert.Equal(DecorateFlowControlKind.Goto, flow.Kind);
        Assert.Equal("Spawn", flow.GotoLabel);
        Assert.Equal(1, flow.GotoOffset);
    }

    [Fact]
    public void Parse_StateWithNoAction_DoesNotConsumeNextStatesSpriteAsAction()
    {
        const string decorate = """
            actor NoActionTest
            {
                States
                {
                Missile:
                    POSS E 8
                    POSS F 8 A_PosAttack
                }
            }
            """;

        DecorateActor actor = Assert.Single(DecorateParser.Parse(decorate));

        DecorateStateDefinition first = Assert.IsType<DecorateStateDefinition>(actor.States[1]);
        Assert.Null(first.ActionFunction);

        DecorateStateDefinition second = Assert.IsType<DecorateStateDefinition>(actor.States[2]);
        Assert.Equal("POSS", second.Sprite);
        Assert.Equal("A_PosAttack", second.ActionFunction);
    }

    [Fact]
    public void Parse_AnonymousActionBlock_CapturesRawUnparsedText()
    {
        const string decorate = """
            actor AnonymousBlockTest
            {
                States
                {
                Spawn:
                    TNT1 A 0
                    {
                        A_Chase;
                        A_FaceTarget;
                    }
                }
            }
            """;

        DecorateActor actor = Assert.Single(DecorateParser.Parse(decorate));

        DecorateStateDefinition state = Assert.IsType<DecorateStateDefinition>(actor.States[1]);
        Assert.Null(state.ActionFunction);
        Assert.NotNull(state.RawActionBlock);
        Assert.Contains("A_Chase", state.RawActionBlock);
        Assert.Contains("A_FaceTarget", state.RawActionBlock);
    }

    [Fact]
    public void Parse_LineAndBlockComments_AreStrippedAndDoNotBreakParsing()
    {
        const string decorate = """
            actor CommentTest // trailing line comment
            {
                /* a block comment
                   spanning multiple lines */
                Health 20 // another comment
                States
                {
                Spawn:
                    TNT1 A 0 // no action here
                    stop
                }
            }
            """;

        DecorateActor actor = Assert.Single(DecorateParser.Parse(decorate));

        Assert.Equal("CommentTest", actor.Name);
        Assert.Equal(["20"], actor.Properties["Health"].Arguments);
        Assert.Contains(actor.States, static entry => entry is DecorateFlowControl { Kind: DecorateFlowControlKind.Stop });
    }

    [Fact]
    public void Parse_MultipleActors_ReturnsOneEntryPerActor()
    {
        const string decorate = """
            actor First 1
            {
            }
            actor Second 2
            {
            }
            """;

        List<DecorateActor> actors = DecorateParser.Parse(decorate);

        Assert.Equal(2, actors.Count);
        Assert.Equal("First", actors[0].Name);
        Assert.Equal("Second", actors[1].Name);
    }
}
