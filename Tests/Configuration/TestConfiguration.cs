// AI Assisted
using System.Text.Json;

namespace Tests;

public sealed class TestConfiguration
{
    public string DukeNukemDirectory { get; init; } = "";

    public string DoomDirectory { get; init; } = "";

    public static TestConfiguration Current { get; } = Load();

    private static TestConfiguration Load()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "testsettings.json");

        if (!File.Exists(path))
        {
            return new TestConfiguration();
        }

        using FileStream stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<TestConfiguration>(stream) ?? new TestConfiguration();
    }
}
