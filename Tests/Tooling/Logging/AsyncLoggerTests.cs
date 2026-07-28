using RenderingEngine.Tooling;

namespace Tests;

public class AsyncLoggerTests
{
    private static string ReadLogFile()
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LOG.txt");
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [Fact]
    public void AddLog_ForEachSeverity_CompletesWithoutThrowing()
    {
        foreach (LogSeverity severity in Enum.GetValues<LogSeverity>())
        {
            AsyncLogger.Default.AddLog(severity, $"severity test {severity}");
        }

        AsyncLogger.Default.WaitSync();
    }

    [Fact]
    public void AddLog_MessageWithUniqueMarker_AppearsInLogFileAfterWaitSync()
    {
        string marker = $"marker-{Guid.NewGuid()}";

        AsyncLogger.Default.AddLog(LogSeverity.Info, marker);
        AsyncLogger.Default.WaitSync();

        string contents = ReadLogFile();
        Assert.Contains(marker, contents);
    }

    [Fact]
    public void AddLog_WithException_ExceptionMessageAppearsInLogFile()
    {
        string marker = $"exmarker-{Guid.NewGuid()}";
        var exception = new InvalidOperationException($"boom-{marker}");

        AsyncLogger.Default.AddLog(LogSeverity.Error, marker, exception);
        AsyncLogger.Default.WaitSync();

        string contents = ReadLogFile();
        Assert.Contains(marker, contents);
        Assert.Contains($"boom-{marker}", contents);
    }

    [Fact]
    public void AddLog_WithExceptionHavingInnerExceptionChain_AllMessagesInChainAppearInLogFile()
    {
        string marker = $"chainmarker-{Guid.NewGuid()}";
        var innerMost = new InvalidOperationException($"innermost-{marker}");
        var middle = new InvalidOperationException($"middle-{marker}", innerMost);
        var outer = new InvalidOperationException($"outer-{marker}", middle);

        AsyncLogger.Default.AddLog(LogSeverity.Error, marker, outer);
        AsyncLogger.Default.WaitSync();

        string contents = ReadLogFile();
        Assert.Contains($"outer-{marker}", contents);
        Assert.Contains($"middle-{marker}", contents);
        Assert.Contains($"innermost-{marker}", contents);
    }

    [Fact]
    public void AddLog_ConcurrentCallsFromMultipleThreads_DoNotThrowAndAllMarkersEventuallyAppearInLogFile()
    {
        string baseMarker = $"concurrent-{Guid.NewGuid()}";
        const int threadCount = 8;

        Parallel.For(0, threadCount, i =>
        {
            AsyncLogger.Default.AddLog(LogSeverity.Debug, $"{baseMarker}-{i}");
        });

        AsyncLogger.Default.WaitSync();

        string contents = ReadLogFile();
        for (int i = 0; i < threadCount; i++)
        {
            Assert.Contains($"{baseMarker}-{i}", contents);
        }
    }

    [Fact]
    public void WaitSync_CalledWithNoPendingLogs_ReturnsPromptlyWithoutThrowing()
    {
        AsyncLogger.Default.WaitSync();
        AsyncLogger.Default.WaitSync();
    }
}
