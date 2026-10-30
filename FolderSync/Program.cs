// FolderSync - keeps a replica folder as an exact one-way copy of a source folder.
//
// Usage:
//   FolderSync --source <path> --replica <path> --interval <seconds> --log <path>
//
// Example:
//   FolderSync --source C:\Photos --replica D:\PhotosCopy --interval 10 --log C:\sync.log

using FolderSync;

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

// The arguments come in pairs: --source <path> --replica <path> ...
string? sourcePath = null;
string? replicaPath = null;
string? logFilePath = null;
int intervalSeconds = 0;

for (int i = 0; i < args.Length; i += 2)
{
    // Every argument must have a value after it.
    if (i + 1 >= args.Length)
    {
        Console.WriteLine($"Error: missing value for '{args[i]}'.");
        return 1;
    }

    switch (args[i].ToLower())
    {
        case "--source":
            sourcePath = args[i + 1];
            break;

        case "--replica":
            replicaPath = args[i + 1];
            break;

        case "--interval":
            if (!int.TryParse(args[i + 1], out intervalSeconds) || intervalSeconds <= 0)
            {
                Console.WriteLine($"Error: interval must be a positive number of seconds, got '{args[i + 1]}'.");
                return 1;
            }
            break;

        case "--log":
            logFilePath = args[i + 1];
            break;

        default:
            Console.WriteLine($"Error: unknown argument '{args[i]}'.");
            return 1;
    }
}

// Check that we got everything we need.
// (a valid interval is never 0, so 0 here simply means "was not provided")
if (sourcePath == null || replicaPath == null || logFilePath == null || intervalSeconds == 0)
{
    Console.WriteLine("Error: --source, --replica, --interval and --log are all required.");
    Console.WriteLine();
    PrintUsage();
    return 1;
}

if (!Directory.Exists(sourcePath))
{
    Console.WriteLine($"Error: source folder does not exist: {sourcePath}");
    return 1;
}

if (File.Exists(replicaPath))
{
    Console.WriteLine($"Error: replica path points to an existing file: {replicaPath}");
    return 1;
}

// Source and replica must be two different folders.
if (IsSameFolder(sourcePath, replicaPath))
{
    Console.WriteLine("Error: source and replica cannot be the same folder.");
    return 1;
}

var logger = new Logger(logFilePath);
var synchronizer = new FolderSynchronizer(sourcePath, replicaPath, logger);

logger.Info($"Started. Source: {sourcePath}, replica: {replicaPath}, interval: {intervalSeconds} s.");
Console.WriteLine("Synchronizing, press Ctrl+C to stop.");

bool stopRequested = false;

Console.CancelKeyPress += (sender, e) =>
{
    // By cancelling the default behaviour we let the program finish
    // what it is currently doing instead of dying in the middle of
    // a file copy.
    e.Cancel = true;
    stopRequested = true;
};

// First pass right away, so the replica is up to date immediately.
synchronizer.RunOnce();

while (!stopRequested)
{
    // Sleep in one-second steps, so that Ctrl+C is noticed quickly
    // instead of waiting for the whole interval to pass.
    for (int secondsWaited = 0; secondsWaited < intervalSeconds && !stopRequested; secondsWaited++)
    {
        Thread.Sleep(1000);
    }

    if (stopRequested)
    {
        break;
    }

    synchronizer.RunOnce();
}

logger.Info("Stopped.");
return 0;

static void PrintUsage()
{
    Console.WriteLine("Makes the replica folder an exact copy of the source folder,");
    Console.WriteLine("and keeps it in sync until the program is stopped.");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  FolderSync --source <path> --replica <path> --interval <seconds> --log <path>");
    Console.WriteLine();
    Console.WriteLine("Example:");
    Console.WriteLine("  FolderSync --source C:\\Photos --replica D:\\PhotosCopy --interval 10 --log C:\\sync.log");
}

static bool IsSameFolder(string pathA, string pathB)
{
    // Windows paths don't care about letter case, and a trailing
    // slash shouldn't make one folder look like two different ones.
    string fullA = Path.TrimEndingDirectorySeparator(Path.GetFullPath(pathA));
    string fullB = Path.TrimEndingDirectorySeparator(Path.GetFullPath(pathB));

    return string.Equals(fullA, fullB, StringComparison.OrdinalIgnoreCase);
}
