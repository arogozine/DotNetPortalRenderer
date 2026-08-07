// AI Assisted
namespace Tests;

public sealed class SkipIfDirectoryNotSetFactAttribute : FactAttribute
{
    public SkipIfDirectoryNotSetFactAttribute(TestDataDirectory directory)
    {
        Skip = TestDataDirectories.GetSkipReason(directory);
    }
}

public sealed class SkipIfDirectoryNotSetTheoryAttribute : TheoryAttribute
{
    public SkipIfDirectoryNotSetTheoryAttribute(TestDataDirectory directory)
    {
        Skip = TestDataDirectories.GetSkipReason(directory);
    }
}
