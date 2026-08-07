// AI Assisted
namespace Tests;

public enum TestDataDirectory
{
    DukeNukem,
    Doom,
}

internal static class TestDataDirectories
{
    public static string GetPath(TestDataDirectory directory) => directory switch
    {
        TestDataDirectory.DukeNukem => TestConfiguration.Current.DukeNukemDirectory,
        TestDataDirectory.Doom => TestConfiguration.Current.DoomDirectory,
        _ => throw new ArgumentOutOfRangeException(nameof(directory)),
    };

    public static string? GetSkipReason(TestDataDirectory directory) =>
        string.IsNullOrWhiteSpace(GetPath(directory))
            ? $"{directory} directory is not configured in testsettings.json."
            : null;
}
