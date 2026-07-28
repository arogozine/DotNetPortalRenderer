using RenderingEngine.Tooling;

namespace Tests;

public class SimpleObjectPoolTests
{
    private sealed class Widget
    {
        public int ResetCount;
    }

    [Fact]
    public void GetOrCreate_FirstAccessAtIndex_LazilyConstructsAndReturnsNonNullInstance()
    {
        var pool = new SimpleObjectPool<Widget>(4, w => w.ResetCount = 0);

        Widget widget = pool.GetOrCreate(2);

        Assert.NotNull(widget);
    }

    [Fact]
    public void GetOrCreate_SubsequentCallsWithSameIndex_ReturnTheSameInstance()
    {
        var pool = new SimpleObjectPool<Widget>(4, w => w.ResetCount = 0);

        Widget first = pool.GetOrCreate(1);
        Widget second = pool.GetOrCreate(1);

        Assert.Same(first, second);
    }

    [Fact]
    public void GetOrCreate_DifferentIndices_ReturnDistinctInstances()
    {
        var pool = new SimpleObjectPool<Widget>(4, w => w.ResetCount = 0);

        Widget first = pool.GetOrCreate(0);
        Widget second = pool.GetOrCreate(1);

        Assert.NotSame(first, second);
    }

    [Fact]
    public void Reset_InvokesResetActionOnceForEachPreviouslyCreatedNonNullSlot()
    {
        var pool = new SimpleObjectPool<Widget>(4, w => w.ResetCount++);

        Widget a = pool.GetOrCreate(0);
        Widget b = pool.GetOrCreate(2);

        pool.Reset();

        Assert.Equal(1, a.ResetCount);
        Assert.Equal(1, b.ResetCount);
    }

    [Fact]
    public void Reset_DoesNotNullSlots_SubsequentGetOrCreateReturnsSameInstanceNotANewOne()
    {
        var pool = new SimpleObjectPool<Widget>(4, w => w.ResetCount = 0);

        Widget first = pool.GetOrCreate(0);

        pool.Reset();

        Widget afterReset = pool.GetOrCreate(0);

        Assert.Same(first, afterReset);
    }

    [Fact]
    public void Reset_SkipsNeverAccessedSlotsWithoutInvokingResetOrThrowing()
    {
        var pool = new SimpleObjectPool<Widget>(4, w => w.ResetCount++);

        pool.GetOrCreate(0);

        pool.Reset();
    }

    [Fact]
    public void GetOrCreate_NegativeIndexThrowsIndexOutOfRangeException()
    {
        var pool = new SimpleObjectPool<Widget>(4, w => w.ResetCount = 0);

        Assert.Throws<IndexOutOfRangeException>(() => pool.GetOrCreate(-1));
    }

    [Fact]
    public void GetOrCreate_IndexEqualToOrGreaterThanCountThrowsIndexOutOfRangeException()
    {
        var pool = new SimpleObjectPool<Widget>(4, w => w.ResetCount = 0);

        Assert.Throws<IndexOutOfRangeException>(() => pool.GetOrCreate(4));
        Assert.Throws<IndexOutOfRangeException>(() => pool.GetOrCreate(100));
    }
}
