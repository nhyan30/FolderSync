# FolderSync

A small command-line tool that keeps a **replica** folder as an exact one-way copy of a
**source** folder. Written in C# (.NET 8).

## What it does

- Compares the source and the replica and makes the replica match the source:
  new and changed files are copied, files and folders that no longer exist in
  the source are deleted from the replica.
- To decide if a file changed, it first compares file sizes and falls back to
  comparing MD5 hashes when the sizes are equal.
- Repeats the synchronization every N seconds until stopped with Ctrl+C.
- Every create / copy / delete operation is written to the console and to the log file.

## Solution layout

| Folder | What's inside |
|--------|---------------|
| `FolderSync/` | the console application |
| `FolderSync.Tests/` | xUnit tests for the sync logic and the logger |

## Build

Requires the .NET 8 SDK.

```
dotnet build
```

## Usage

```
FolderSync --source <path> --replica <path> --interval <seconds> --log <path>
```

Example (from the solution root):

```
dotnet run --project FolderSync -- --source C:\Photos --replica D:\PhotosCopy --interval 10 --log C:\sync.log
```

| Argument | Meaning |
|----------|---------|
| `--source` | folder to copy from (must already exist) |
| `--replica` | folder that becomes a copy of the source (created if missing) |
| `--interval` | pause between synchronization passes, in seconds |
| `--log` | log file path |

## Tests

```
dotnet test
```

The tests run the synchronizer against throw-away folders in the temp directory
and cover the main scenarios: first copy, changed file (different size, and same
size but different content), deleted files and folders, extra files in the
replica, names with special characters, and a no-op second pass.

## Notes

- The first pass runs immediately on start.
- If a single file is locked by another program, the error is logged and the
  rest of the pass continues; the file is retried on the next pass.
- The tool itself uses only the standard .NET library (MD5 comes from
  `System.Security.Cryptography`); the separate test project uses xUnit.
