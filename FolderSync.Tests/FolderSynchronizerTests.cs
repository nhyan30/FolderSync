using FolderSync;

namespace FolderSync.Tests;

// Tests for the synchronization logic. Every test gets its own pair of
// throw-away folders in the temp directory, so tests can't disturb each other.
public class FolderSynchronizerTests : IDisposable
{
    private readonly string _testRoot;
    private readonly string _source;
    private readonly string _replica;
    private readonly string _logFile;

    public FolderSynchronizerTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "FolderSyncTests_" + Guid.NewGuid().ToString("N"));
        _source = Path.Combine(_testRoot, "source");
        _replica = Path.Combine(_testRoot, "replica");
        _logFile = Path.Combine(_testRoot, "sync.log");

        Directory.CreateDirectory(_source);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    // Runs one synchronization pass with the folders of this test.
    private void RunOneSyncPass()
    {
        var logger = new Logger(_logFile);
        new FolderSynchronizer(_source, _replica, logger).RunOnce();
    }

    // Counts how many "Copied file" lines the log has so far. Used to
    // check that unchanged files are not copied over and over again.
    private int CountCopyOperations()
    {
        return File.ReadAllLines(_logFile).Count(line => line.Contains("Copied file"));
    }

    [Fact]
    public void FirstPass_CopiesAllFilesAndSubfolders()
    {
        Directory.CreateDirectory(Path.Combine(_source, "docs"));
        File.WriteAllText(Path.Combine(_source, "a.txt"), "one");
        File.WriteAllText(Path.Combine(_source, "docs", "b.txt"), "two");

        RunOneSyncPass();

        Assert.Equal("one", File.ReadAllText(Path.Combine(_replica, "a.txt")));
        Assert.Equal("two", File.ReadAllText(Path.Combine(_replica, "docs", "b.txt")));
    }

    [Fact]
    public void ReplicaFolder_DoesNotNeedToExistBeforehand()
    {
        File.WriteAllText(Path.Combine(_source, "a.txt"), "one");

        RunOneSyncPass();

        Assert.True(Directory.Exists(_replica));
        Assert.True(File.Exists(Path.Combine(_replica, "a.txt")));
    }

    [Fact]
    public void ChangedFile_IsUpdatedInReplica()
    {
        File.WriteAllText(Path.Combine(_source, "a.txt"), "one");
        RunOneSyncPass();

        File.WriteAllText(Path.Combine(_source, "a.txt"), "one-two-three");
        RunOneSyncPass();

        Assert.Equal("one-two-three", File.ReadAllText(Path.Combine(_replica, "a.txt")));
    }

    [Fact]
    public void SameSizeButDifferentContent_IsUpdatedInReplica()
    {
        // "aaaa" and "bbbb" have the same length, so the size check alone
        // can't catch this change - only the hash comparison can.
        File.WriteAllText(Path.Combine(_source, "a.txt"), "aaaa");
        RunOneSyncPass();
        Assert.Equal("aaaa", File.ReadAllText(Path.Combine(_replica, "a.txt")));

        File.WriteAllText(Path.Combine(_source, "a.txt"), "bbbb");
        RunOneSyncPass();

        Assert.Equal("bbbb", File.ReadAllText(Path.Combine(_replica, "a.txt")));
    }

    [Fact]
    public void UnchangedFiles_AreNotCopiedAgain()
    {
        File.WriteAllText(Path.Combine(_source, "a.txt"), "one");
        RunOneSyncPass();

        int copiesAfterFirstPass = CountCopyOperations();

        RunOneSyncPass();

        Assert.Equal(copiesAfterFirstPass, CountCopyOperations());
    }

    [Fact]
    public void FileDeletedFromSource_IsRemovedFromReplica()
    {
        File.WriteAllText(Path.Combine(_source, "a.txt"), "one");
        File.WriteAllText(Path.Combine(_source, "b.txt"), "two");
        RunOneSyncPass();

        File.Delete(Path.Combine(_source, "a.txt"));
        RunOneSyncPass();

        Assert.False(File.Exists(Path.Combine(_replica, "a.txt")));
        Assert.True(File.Exists(Path.Combine(_replica, "b.txt")));
    }

    [Fact]
    public void FileThatExistsOnlyInReplica_IsRemoved()
    {
        File.WriteAllText(Path.Combine(_source, "a.txt"), "one");
        RunOneSyncPass();

        File.WriteAllText(Path.Combine(_replica, "extra.txt"), "junk");
        RunOneSyncPass();

        Assert.False(File.Exists(Path.Combine(_replica, "extra.txt")));
    }

    [Fact]
    public void FolderDeletedFromSource_IsRemovedWithAllItsFiles()
    {
        Directory.CreateDirectory(Path.Combine(_source, "docs"));
        File.WriteAllText(Path.Combine(_source, "docs", "b.txt"), "two");
        RunOneSyncPass();

        Directory.Delete(Path.Combine(_source, "docs"), recursive: true);
        RunOneSyncPass();

        Assert.False(Directory.Exists(Path.Combine(_replica, "docs")));
    }

    [Fact]
    public void EmptyFolderInSource_IsCreatedInReplica()
    {
        Directory.CreateDirectory(Path.Combine(_source, "empty"));

        RunOneSyncPass();

        Assert.True(Directory.Exists(Path.Combine(_replica, "empty")));
    }

    [Fact]
    public void NamesWithSpacesAndSpecialCharacters_AreHandled()
    {
        string folderName = "docs & notes (2024)";
        string fileName = "my report - final version.txt";

        Directory.CreateDirectory(Path.Combine(_source, folderName));
        File.WriteAllText(Path.Combine(_source, folderName, fileName), "hello");

        RunOneSyncPass();

        Assert.Equal("hello", File.ReadAllText(Path.Combine(_replica, folderName, fileName)));
    }
}
