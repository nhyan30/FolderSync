namespace FolderSync;

// Writes every message both to the console and to the log file.
// public, so that the test project can use this class
public class Logger
{
    private readonly string _logFilePath;

    public Logger(string logFilePath)
    {
        // The folder of the log file may not exist yet (for example when
        // the path is C:\logs\sync.log), so we create it first.
        _logFilePath = Path.GetFullPath(logFilePath);

        string? logFolder = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrEmpty(logFolder))
        {
            Directory.CreateDirectory(logFolder);
        }
    }

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Error(string message)
    {
        Write("ERROR", message);
    }

    private void Write(string level, string message)
    {
        string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";

        Console.WriteLine(line);
        File.AppendAllText(_logFilePath, line + Environment.NewLine);
    }
}
