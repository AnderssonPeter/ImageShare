using ImageShare.Authentication;
using ImageShare.Endpoints;
using Microsoft.AspNetCore.Http;

namespace ImageShare.Tests;

public class FolderEndpointsTests
{
    private sealed class TestUser : IUser
    {
        public bool IsAuthenticated { get; init; } = true;
        public string Name { get; init; } = "test";
        private readonly HashSet<string> _allowedFolders = [];

        public TestUser Allow(string folder)
        {
            _allowedFolders.Add(folder);
            return this;
        }

        public bool CanAccessFolder(string folder) => _allowedFolders.Contains(folder);
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"imageshare-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static void CreateFile(string dir, string name) =>
        File.WriteAllText(Path.Combine(dir, name), string.Empty);

    private static void CreateDir(string dir, string name) =>
        Directory.CreateDirectory(Path.Combine(dir, name));

    private static List<FolderEntry> GetEntries(IResult result) =>
        (List<FolderEntry>)((Microsoft.AspNetCore.Http.HttpResults.Ok<List<FolderEntry>>)result).Value!;

    [Test]
    public async Task ListFolder_Unauthenticated_ReturnsUnauthorized()
    {
        var user = new TestUser { IsAuthenticated = false };
        var result = BrowsingEndpoints.ListFolder("/tmp", string.Empty, user);

        await Assert.That(IsStatusCode(result, 401)).IsTrue();
    }

    [Test]
    public async Task ListFolder_NonExistentPath_ReturnsNotFound()
    {
        var user = new TestUser();
        var result = BrowsingEndpoints.ListFolder("/nonexistent-path-xyz", string.Empty, user);

        await Assert.That(IsStatusCode(result, 404)).IsTrue();
    }

    [Test]
    public async Task ListFolder_PathTraversal_ReturnsBadRequest()
    {
        using var _ = new DisposableTempDir(out var basePath);
        var user = new TestUser();
        var result = BrowsingEndpoints.ListFolder(basePath, "../etc", user);

        await Assert.That(IsStatusCode(result, 400)).IsTrue();
    }

    [Test]
    public async Task ListFolder_Root_FiltersFoldersByAccess()
    {
        using var _ = new DisposableTempDir(out var basePath);
        CreateDir(basePath, "allowed-folder");
        CreateDir(basePath, "blocked-folder");
        CreateFile(basePath, "file.txt");

        var user = new TestUser().Allow("allowed-folder");
        var result = BrowsingEndpoints.ListFolder(basePath, string.Empty, user);

        var entries = GetEntries(result);
        await Assert.That(entries).IsNotNull();
        await Assert.That(entries.Count).IsEqualTo(2);

        var folder = entries.Single(e => e.Name == "allowed-folder");
        await Assert.That(folder.Type).IsEqualTo(EntryType.Folder);

        var file = entries.Single(e => e.Name == "file.txt");
        await Assert.That(file.Type).IsEqualTo(EntryType.File);

        await Assert.That(entries.Any(e => e.Name == "blocked-folder")).IsFalse();
    }

    [Test]
    public async Task ListFolder_Root_AllFoldersBlocked_ReturnsOnlyFiles()
    {
        using var _ = new DisposableTempDir(out var basePath);
        CreateDir(basePath, "secret");
        CreateFile(basePath, "public.txt");

        var user = new TestUser();
        var result = BrowsingEndpoints.ListFolder(basePath, string.Empty, user);

        var entries = GetEntries(result);
        await Assert.That(entries.Count).IsEqualTo(1);
        await Assert.That(entries[0].Name).IsEqualTo("public.txt");
        await Assert.That(entries[0].Type).IsEqualTo(EntryType.File);
    }

    [Test]
    public async Task ListFolder_Subfolder_DoesNotFilterByAccess()
    {
        using var _ = new DisposableTempDir(out var basePath);
        CreateDir(basePath, "allowed");
        var subDir = Path.Combine(basePath, "allowed");
        CreateDir(subDir, "sub-secret");
        CreateDir(subDir, "sub-public");
        CreateFile(subDir, "sub-file.txt");

        var user = new TestUser().Allow("allowed");
        var result = BrowsingEndpoints.ListFolder(basePath, "allowed", user);

        var entries = GetEntries(result);
        await Assert.That(entries.Count).IsEqualTo(3);

        var folder1 = entries.Single(e => e.Name == "sub-secret");
        await Assert.That(folder1.Type).IsEqualTo(EntryType.Folder);
        var folder2 = entries.Single(e => e.Name == "sub-public");
        await Assert.That(folder2.Type).IsEqualTo(EntryType.Folder);
    }

    [Test]
    public async Task ListFolder_EmptyDirectory_ReturnsEmptyList()
    {
        using var _ = new DisposableTempDir(out var basePath);
        var user = new TestUser();
        var result = BrowsingEndpoints.ListFolder(basePath, string.Empty, user);

        var entries = GetEntries(result);
        await Assert.That(entries.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ListFolder_SortsFoldersBeforeFiles()
    {
        using var _ = new DisposableTempDir(out var basePath);
        CreateFile(basePath, "a.txt");
        CreateDir(basePath, "z-folder");

        var user = new TestUser().Allow("z-folder");
        var result = BrowsingEndpoints.ListFolder(basePath, string.Empty, user);

        var entries = GetEntries(result);
        await Assert.That(entries[0].Name).IsEqualTo("z-folder");
        await Assert.That(entries[0].Type).IsEqualTo(EntryType.Folder);
        await Assert.That(entries[1].Name).IsEqualTo("a.txt");
        await Assert.That(entries[1].Type).IsEqualTo(EntryType.File);
    }

    [Test]
    public async Task ListFolder_SortsAlphabeticallyWithinType()
    {
        using var _ = new DisposableTempDir(out var basePath);
        CreateDir(basePath, "b-folder");
        CreateDir(basePath, "a-folder");
        CreateFile(basePath, "z-file.txt");
        CreateFile(basePath, "a-file.txt");

        var user = new TestUser().Allow("a-folder").Allow("b-folder");
        var result = BrowsingEndpoints.ListFolder(basePath, string.Empty, user);

        var entries = GetEntries(result);
        await Assert.That(entries[0].Name).IsEqualTo("a-folder");
        await Assert.That(entries[1].Name).IsEqualTo("b-folder");
        await Assert.That(entries[2].Name).IsEqualTo("a-file.txt");
        await Assert.That(entries[3].Name).IsEqualTo("z-file.txt");
    }

    private static bool IsStatusCode(IResult result, int statusCode)
    {
        return result switch
        {
            Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult => statusCode == 401,
            Microsoft.AspNetCore.Http.HttpResults.NotFound => statusCode == 404,
            Microsoft.AspNetCore.Http.HttpResults.BadRequest => statusCode == 400,
            Microsoft.AspNetCore.Http.HttpResults.Ok<List<FolderEntry>> => statusCode == 200,
            _ => false,
        };
    }
}

file sealed class DisposableTempDir : IDisposable
{
    private readonly string _path;

    public DisposableTempDir(out string path)
    {
        _path = Path.Combine(Path.GetTempPath(), $"imageshare-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_path);
        path = _path;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_path, true);
        }
        catch { /* best effort */ }
    }
}
