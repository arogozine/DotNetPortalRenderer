using RenderingEngine.Tooling;

namespace Tests;

public class DynamicObjectPoolTests
{
    private sealed class Widget
    {
        public int Value;
        public int ResetCount;
    }

    [Fact]
    public void GetOrCreate_FirstCall_ConstructsAndReturnsNewNonNullInstance()
    {
        var pool = new DynamicObjectPool<Widget>(w => w.Value = 0);

        Widget widget = pool.GetOrCreate();

        Assert.NotNull(widget);
    }

    [Fact]
    public void GetOrCreate_RepeatedCallsWithoutReset_ReturnDistinctInstancesEachTime()
    {
        var pool = new DynamicObjectPool<Widget>(w => w.Value = 0);

        Widget first = pool.GetOrCreate();
        Widget second = pool.GetOrCreate();
        Widget third = pool.GetOrCreate();

        Assert.NotSame(first, second);
        Assert.NotSame(second, third);
    }

    [Fact]
    public void Reset_InvokesResetActionExactlyOnceForEachPreviouslyCreatedInstance()
    {
        var pool = new DynamicObjectPool<Widget>(w => w.ResetCount++);

        var widgets = new List<Widget>();
        for (int i = 0; i < 5; i++)
        {
            widgets.Add(pool.GetOrCreate());
        }

        pool.Reset();

        foreach (var widget in widgets)
        {
            Assert.Equal(1, widget.ResetCount);
        }
    }

    [Fact]
    public void Reset_ThenRepeatingSameCallSequence_ReusesSameInstancesInSameOrderWithoutNullingSlots()
    {
        var pool = new DynamicObjectPool<Widget>(w => w.Value = 0);

        var firstCycle = new List<Widget>();
        for (int i = 0; i < 5; i++)
        {
            firstCycle.Add(pool.GetOrCreate());
        }

        pool.Reset();

        var secondCycle = new List<Widget>();
        for (int i = 0; i < 5; i++)
        {
            secondCycle.Add(pool.GetOrCreate());
        }

        Assert.Equal(firstCycle, secondCycle);
    }

    [Fact]
    public void GetOrCreate_GrowthAcrossInitialThirtyTwoSlotBoundary_DoesNotThrowAndAllReturnedObjectsAreNonNullAndDistinct()
    {
        var pool = new DynamicObjectPool<Widget>(w => w.Value = 0);

        var widgets = new List<Widget>();
        for (int i = 0; i < 40; i++)
        {
            widgets.Add(pool.GetOrCreate());
        }

        Assert.All(widgets, Assert.NotNull);
        Assert.Equal(widgets.Count, widgets.Distinct().Count());
    }

    [Fact]
    public void Clear_WithoutReset_LeavesCounterRunningSoNextGetOrCreateCallsProduceFreshInstances()
    {
        var pool = new DynamicObjectPool<Widget>(w => w.Value = 0);

        Widget first = pool.GetOrCreate();

        pool.Clear();

        Widget second = pool.GetOrCreate();

        Assert.NotSame(first, second);
    }

    [Fact]
    public void Clear_ThenReset_BehavesLikeFreshPool_FirstSubsequentGetOrCreateConstructsNewInstance()
    {
        var pool = new DynamicObjectPool<Widget>(w => w.Value = 0);

        Widget first = pool.GetOrCreate();

        pool.Clear();
        pool.Reset();

        Widget second = pool.GetOrCreate();

        Assert.NotSame(first, second);
        Assert.NotNull(second);
    }

    [Fact]
    public void GetOrCreate_ConcurrentCallsFromMultipleThreads_DoNotThrowAndAllReturnedObjectsAreNonNull()
    {
        var pool = new DynamicObjectPool<Widget>(w => w.Value = 0);
        var bag = new System.Collections.Concurrent.ConcurrentBag<Widget>();

        Parallel.For(0, 200, _ =>
        {
            bag.Add(pool.GetOrCreate());
        });

        Assert.Equal(200, bag.Count);
        Assert.All(bag, Assert.NotNull);
    }
}
