using System.Security.Cryptography;

namespace FolderSync;

// Compares the source and replica folders and changes the replica
// so that it becomes an exact copy of the source (one way only:
// source -> replica, never the other direction).
// public, so that the test project can use this class
public class FolderSynchronizer
{
    private readonly string _sourcePath;
    private readonly string _replicaPath;
    private readonly Logger _logger;

    public FolderSynchronizer(string sourcePath, string replicaPath, Logger logger)
    {
        _sourcePath = sourcePath;
        _replicaPath = replicaPath;
        _logger = logger;
    }

    // Runs one full synchronization pass over both folder trees.
    public void RunOnce()
    {
        try
        {
            SyncFolder(_sourcePath, _replicaPath);
            _logger.Info("Synchronization pass finished.");
        }
        catch (Exception ex)
        {
            // If we get here, something is wrong with the folders themselves
            // (for example the source folder was deleted while the program
            // was running). We log it and stay alive - the next pass might
            // still work fine.
            _logger.Error($"Synchronization pass failed: {ex.Message}");
        }
    }

    // Makes one folder of the replica match the same folder of the
    // source, then does the same for all of its subfolders.
    private void SyncFolder(string sourceDir, string replicaDir)
    {
        // The matching replica folder is created if it's missing.
        if (!Directory.Exists(replicaDir))
        {
            Directory.CreateDirectory(replicaDir);
            _logger.Info($"Created folder: {replicaDir}");
        }

        // 1. Copy every new or changed file to the replica.
        foreach (string sourceFile in Directory.GetFiles(sourceDir))
        {
            string fileName = Path.GetFileName(sourceFile);
            string replicaFile = Path.Combine(replicaDir, fileName);

            try
            {
                if (NeedsCopy(sourceFile, replicaFile))
                {
                    File.Copy(sourceFile, replicaFile, overwrite: true);
                    _logger.Info($"Copied file: {sourceFile} -> {replicaFile}");
                }
            }
            catch (Exception ex)
            {
                // Usually this means the file was locked by another
                // program. We skip it for now and try again next pass.
                _logger.Error($"Could not copy '{sourceFile}': {ex.Message}");
            }
        }

        // 2. Delete files that exist only in the replica - they were
        //    removed from the source at some point.
        foreach (string replicaFile in Directory.GetFiles(replicaDir))
        {
            string fileName = Path.GetFileName(replicaFile);
            string sourceFile = Path.Combine(sourceDir, fileName);

            if (!File.Exists(sourceFile))
            {
                try
                {
                    File.Delete(replicaFile);
                    _logger.Info($"Deleted file: {replicaFile}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Could not delete '{replicaFile}': {ex.Message}");
                }
            }
        }

        // 3. Do the same for every subfolder of the source.
        foreach (string sourceSubfolder in Directory.GetDirectories(sourceDir))
        {
            string folderName = Path.GetFileName(sourceSubfolder);
            string replicaSubfolder = Path.Combine(replicaDir, folderName);

            try
            {
                SyncFolder(sourceSubfolder, replicaSubfolder);
            }
            catch (Exception ex)
            {
                _logger.Error($"Could not sync folder '{sourceSubfolder}': {ex.Message}");
            }
        }

        // 4. Delete subfolders that exist only in the replica.
        foreach (string replicaSubfolder in Directory.GetDirectories(replicaDir))
        {
            string folderName = Path.GetFileName(replicaSubfolder);
            string sourceSubfolder = Path.Combine(sourceDir, folderName);

            if (!Directory.Exists(sourceSubfolder))
            {
                try
                {
                    Directory.Delete(replicaSubfolder, recursive: true);
                    _logger.Info($"Deleted folder: {replicaSubfolder}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Could not delete folder '{replicaSubfolder}': {ex.Message}");
                }
            }
        }
    }

    // Returns true when a file has to be copied to the replica:
    // either it's not there yet, or its content is different.
    private bool NeedsCopy(string sourceFile, string replicaFile)
    {
        if (!File.Exists(replicaFile))
        {
            return true;
        }

        // Different sizes always mean different content, and checking
        // the size is much cheaper than reading both files.
        if (new FileInfo(sourceFile).Length != new FileInfo(replicaFile).Length)
        {
            return true;
        }

        // Same size is not a proof of same content, so in that case we
        // compare hashes. MD5 is good enough here - it's not used for
        // security, we only need to know if the files are different.
        return GetMd5(sourceFile) != GetMd5(replicaFile);
    }

    // Returns the MD5 hash of a file as a hex string.
    private static string GetMd5(string filePath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);

        byte[] hash = md5.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }
}
