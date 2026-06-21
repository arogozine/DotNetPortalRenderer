using System.Text;
using System.Threading.Channels;

namespace RenderingEngine.Tooling;

public enum LogSeverity
{
    Debug,
    Info,
    Warning,
    Error
}

public sealed class AsyncLogger : IDisposable
{
    public static readonly AsyncLogger Default = new();

    private readonly CancellationTokenSource _cancellationTokenSource;

    private readonly StringBuilder _stringBuilder = new();
    private readonly Channel<string> _channel = Channel.CreateUnbounded<string>();

    private FileStream? _loggerStream;

    private AsyncLogger()
    {
        _cancellationTokenSource = new();
        _loggerStream = InitializeStream();
        InitializeTask();
    }

    public void InitializeTask()
    {
        if (_loggerStream is null)
        {
            return;
        }

        _ = Task.Run(LoggingThread, _cancellationTokenSource.Token);
    }

    private async Task LoggingThread()
    {
        CancellationToken cancellationToken = _cancellationTokenSource.Token;

        try
        {
            using var writer = new StreamWriter(_loggerStream!) { AutoFlush = true };

            while (!cancellationToken.IsCancellationRequested)
            {
                await foreach (string logEntry in _channel.Reader.ReadAllAsync(cancellationToken))
                {
                    Debug.WriteLine(logEntry);

                    await writer.WriteLineAsync(logEntry.AsMemory(), cancellationToken);
                }
            }
        }
        catch (ObjectDisposedException)
        {
            return;
        }
    }

    public void AddLog(LogSeverity logSeverity, string message, Exception? ex = null)
    {
        if (_loggerStream is null)
        {
            return;
        }

        _ = _stringBuilder.Clear();
        _ = _stringBuilder
            .Append(DateTime.Now.ToString("HH:mm tt"))
            .Append(": ")
            .Append(logSeverity.ToString())
            .Append(" - ")
            .Append(message)
            .Append('.');

        for (Exception? exp = ex; exp != null; exp = exp.InnerException)
        {
            _ = _stringBuilder
                .AppendLine()
                .Append(exp.Message)
                .AppendLine()
                .Append(exp.StackTrace);
        }

        _ = _channel.Writer.TryWrite(_stringBuilder.ToString());
    }

    public void WaitSync()
    {
        if (_loggerStream is null)
        {
            return;
        }

        // Wait for 1 second for all writes to finish
        for (int i = 0; i < 10 && _channel.Reader.Count > 0; i ++)
        {
            Thread.Sleep(100);
        }
    }

    private static FileStream? InitializeStream()
    {
        const string LogName = "LOG.txt";

        string runDir = AppDomain.CurrentDomain.BaseDirectory;

        if (string.IsNullOrWhiteSpace(runDir) || !Directory.Exists(runDir))
        {
            Debug.WriteLine("Cannot determine the running directory.");
            return null;
        }

        string filePath = Path.Combine(runDir, LogName);

        return new FileStream(filePath, FileMode.OpenOrCreate | FileMode.Append, FileAccess.Write, FileShare.Read, 4096, true);
    }

    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        _loggerStream?.Dispose();
        _loggerStream = null;

        GC.SuppressFinalize(this);
    }

    ~AsyncLogger() => Dispose();
}
