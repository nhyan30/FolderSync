using FolderSync;

namespace FolderSync.Tests;

// Tests for the Logger on its own, so a broken log format is easy to spot.
public class LoggerTests : IDisposable
{
    private readonly string _testRoot;

    public LoggerTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "FolderSyncLoggerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    [Fact]
    public void Messages_AreWrittenToTheLogFile()
    {
        string logFile = Path.Combine(_testRoot, "sync.log");
        var logger = new Logger(logFile);

        logger.Info("something happened");

        string[] lines = File.ReadAllLines(logFile);
        Assert.Single(lines);
        Assert.Contains("[INFO]", lines[0]);
        Assert.EndsWith("something happened", lines[0]);
    }

    [Fact]
    public void MissingLogFolder_IsCreatedAutomatically()
    {
        // The "logs" folder does not exist yet - the Logger has to create it.
        string logFile = Path.Combine(_testRoot, "logs", "sync.log");
        var logger = new Logger(logFile);

        logger.Info("hello");

        Assert.True(File.Exists(logFile));
    }
}
