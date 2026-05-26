using Ambdev.Interpreter.AST;
using Ambdev.Interpreter.Runtime;

public class ChainTests
{
    [Fact]
    public void ChainIndexAndName()
    {
        var i = Fixture.Run("chain[3, MyChain]");
        var c = i.Chain(3);
        Assert.Equal(3, c.Index);
        Assert.Equal("MyChain", c.Name);
    }

    [Fact]
    public void ChainNoSteps()
    {
        var i = Fixture.Run("chain[1, Empty]");
        Assert.Empty(i.Chain(1).Steps);
    }

    [Fact]
    public void ChainAlwaysStep()
    {
        var i = Fixture.Run("chain[1, C] - event 5");
        Assert.Equal(StepPrefix.Always, i.Chain(1).Steps[0].Prefix);
    }

    [Fact]
    public void ChainIfSuccessStep()
    {
        var i = Fixture.Run("chain[1, C] ? chain 2");
        Assert.Equal(StepPrefix.IfSuccess, i.Chain(1).Steps[0].Prefix);
    }

    [Fact]
    public void ChainIfFailureStep()
    {
        var i = Fixture.Run("chain[1, C] ! chain 3");
        Assert.Equal(StepPrefix.IfFailure, i.Chain(1).Steps[0].Prefix);
    }

    [Fact]
    public void ChainEventTarget()
    {
        var i = Fixture.Run("chain[1, C] - event 7");
        var step = i.Chain(1).Steps[0];
        Assert.Equal(StepTarget.Event, step.Target);
        Assert.Equal(7, step.TargetIndex);
    }

    [Fact]
    public void ChainChainTarget()
    {
        var i = Fixture.Run("chain[1, C] - chain 2");
        var step = i.Chain(1).Steps[0];
        Assert.Equal(StepTarget.Chain, step.Target);
        Assert.Equal(2, step.TargetIndex);
    }

    [Fact]
    public void ChainMultipleSteps()
    {
        var i = Fixture.Run("chain[1, C] - event 1 - chain 2 ? chain 5 ! chain 7");
        var steps = i.Chain(1).Steps;
        Assert.Equal(4, steps.Count);
        Assert.Equal((StepPrefix.Always,     StepTarget.Event, 1), (steps[0].Prefix, steps[0].Target, steps[0].TargetIndex));
        Assert.Equal((StepPrefix.Always,     StepTarget.Chain, 2), (steps[1].Prefix, steps[1].Target, steps[1].TargetIndex));
        Assert.Equal((StepPrefix.IfSuccess,  StepTarget.Chain, 5), (steps[2].Prefix, steps[2].Target, steps[2].TargetIndex));
        Assert.Equal((StepPrefix.IfFailure,  StepTarget.Chain, 7), (steps[3].Prefix, steps[3].Target, steps[3].TargetIndex));
    }

    [Fact]
    public void ChainSequential()
    {
        // Multiple chains defined in one file
        var i = Fixture.Run(
            "chain[1, First] - event 1 " +
            "chain[2, Second] - chain 1");
        Assert.Single(i.Chain(1).Steps);
        Assert.Single(i.Chain(2).Steps);
    }

    [Fact]
    public void DuplicateChainThrows()
        => Assert.Throws<AmbdevRuntimeException>(() =>
            Fixture.Run("chain[1, A] chain[1, B]"));

    [Fact]
    public void ChainStepEventByName()
    {
        var src =
            "etype[1, Move]: - 0: byte actorId " +
            "event[7, HeroMoves] = etype[1] - actorId: 1 " +
            "chain[1, C] - event HeroMoves";
        var step = Fixture.Run(src).Chain(1).Steps[0];
        Assert.Equal(StepTarget.Event, step.Target);
        Assert.Equal(7, step.TargetIndex);
    }

    [Fact]
    public void ChainStepChainByName()
    {
        var src =
            "chain[2, Target] " +
            "chain[1, C] - chain Target";
        var step = Fixture.Run(src).Chain(1).Steps[0];
        Assert.Equal(StepTarget.Chain, step.Target);
        Assert.Equal(2, step.TargetIndex);
    }

    [Fact]
    public void ChainStepForwardChainReference()
    {
        var src =
            "chain[1, First] ? chain Second " +
            "chain[2, Second] - chain First";
        var i = Fixture.Run(src);
        Assert.Equal(2, i.Chain(1).Steps[0].TargetIndex);
        Assert.Equal(1, i.Chain(2).Steps[0].TargetIndex);
    }

    [Fact]
    public void ChainStepUnknownEventNameThrows()
        => Assert.Throws<AmbdevRuntimeException>(() =>
            Fixture.Run("chain[1, C] - event NoSuchEvent"));

    [Fact]
    public void ChainStepUnknownChainNameThrows()
        => Assert.Throws<AmbdevRuntimeException>(() =>
            Fixture.Run("chain[1, C] - chain NoSuchChain"));
}
