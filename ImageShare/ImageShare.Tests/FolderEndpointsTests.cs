using ImageShare.Authentication;
using ImageShare.Browsing;
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

    private static void CreateFile(string dir, string name) =>
        File.WriteAllText(Path.Combine(dir, name), string.Empty);

    private static void CreateDir(string dir, string name) =>
        Directory.CreateDirectory(Path.Combine(dir, name));

    private static PaginatedResult<FolderEntry> GetResult(IResult result) =>
        (PaginatedResult<FolderEntry>)((Microsoft.AspNetCore.Http.HttpResults.Ok<PaginatedResult<FolderEntry>>)result).Value!;

    private const int Page = 1;
    private const int PageSize = 50;

    [Test]
    public async Task GetEntries_Unauthenticated_ReturnsUnauthorized()
    {
        var user = new TestUser { IsAuthenticated = false };
        var result = BrowsingEndpoints.GetEntries("/tmp", string.Empty, user, Page, PageSize);

        await Assert.That(IsStatusCode(result, 401)).IsTrue();
    }

    [Test]
    public async Task GetEntries_NonExistentPath_ReturnsNotFound()
    {
        var user = new TestUser();
        var result = BrowsingEndpoints.GetEntries("/nonexistent-path-xyz", string.Empty, user, Page, PageSize);

        await Assert.That(IsStatusCode(result, 404)).IsTrue();
    }

    [Test]
    public async Task GetEntries_PathTraversal_ReturnsBadRequest()
    {
        using var _ = new DisposableTempDir(out var basePath);
        var user = new TestUser();
        var result = BrowsingEndpoints.GetEntries(basePath, "../etc", user, Page, PageSize);

        await Assert.That(IsStatusCode(result, 400)).IsTrue();
    }

    [Test]
    [Arguments(0, 10)]
    [Arguments(1, 0)]
    [Arguments(1, 501)]
    public async Task GetEntries_InvalidPagination_ReturnsBadRequest(int page, int pageSize)
    {
        using var _ = new DisposableTempDir(out var basePath);
        var user = new TestUser();
        var result = BrowsingEndpoints.GetEntries(basePath, string.Empty, user, page, pageSize);

        await Assert.That(IsStatusCode(result, 400)).IsTrue();
    }

    [Test]
    public async Task GetEntries_Root_FiltersFoldersByAccess()
    {
        using var _ = new DisposableTempDir(out var basePath);
        CreateDir(basePath, "allowed-folder");
        CreateDir(basePath, "blocked-folder");
        CreateFile(basePath, "file.txt");

        var user = new TestUser().Allow("allowed-folder");
        var result = BrowsingEndpoints.GetEntries(basePath, string.Empty, user, Page, PageSize);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items).IsNotNull();
        await Assert.That(paginated.Items.Count).IsEqualTo(2);
        await Assert.That(paginated.TotalCount).IsEqualTo(2);
        await Assert.That(paginated.Page).IsEqualTo(Page);
        await Assert.That(paginated.PageSize).IsEqualTo(PageSize);

        var folder = paginated.Items.Single(e => e.Name == "allowed-folder");
        await Assert.That(folder.Type).IsEqualTo(EntryType.Folder);

        var file = paginated.Items.Single(e => e.Name == "file.txt");
        await Assert.That(file.Type).IsEqualTo(EntryType.File);

        await Assert.That(paginated.Items.Any(e => e.Name == "blocked-folder")).IsFalse();
    }

    [Test]
    public async Task GetEntries_Root_AllFoldersBlocked_ReturnsOnlyFiles()
    {
        using var _ = new DisposableTempDir(out var basePath);
        CreateDir(basePath, "secret");
        CreateFile(basePath, "public.txt");

        var user = new TestUser();
        var result = BrowsingEndpoints.GetEntries(basePath, string.Empty, user, Page, PageSize);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items.Count).IsEqualTo(1);
        await Assert.That(paginated.TotalCount).IsEqualTo(1);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("public.txt");
        await Assert.That(paginated.Items[0].Type).IsEqualTo(EntryType.File);
    }

    [Test]
    public async Task GetEntries_Subfolder_DoesNotFilterByAccess()
    {
        using var _ = new DisposableTempDir(out var basePath);
        CreateDir(basePath, "allowed");
        var subDir = Path.Combine(basePath, "allowed");
        CreateDir(subDir, "sub-secret");
        CreateDir(subDir, "sub-public");
        CreateFile(subDir, "sub-file.txt");

        var user = new TestUser().Allow("allowed");
        var result = BrowsingEndpoints.GetEntries(basePath, "allowed", user, Page, PageSize);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items.Count).IsEqualTo(3);
        await Assert.That(paginated.TotalCount).IsEqualTo(3);

        var folder1 = paginated.Items.Single(e => e.Name == "sub-secret");
        await Assert.That(folder1.Type).IsEqualTo(EntryType.Folder);
        var folder2 = paginated.Items.Single(e => e.Name == "sub-public");
        await Assert.That(folder2.Type).IsEqualTo(EntryType.Folder);
    }

    [Test]
    public async Task GetEntries_EmptyDirectory_ReturnsEmpty()
    {
        using var _ = new DisposableTempDir(out var basePath);
        var user = new TestUser();
        var result = BrowsingEndpoints.GetEntries(basePath, string.Empty, user, Page, PageSize);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items.Count).IsEqualTo(0);
        await Assert.That(paginated.TotalCount).IsEqualTo(0);
    }

    [Test]
    public async Task GetEntries_SortsFoldersBeforeFiles()
    {
        using var _ = new DisposableTempDir(out var basePath);
        CreateFile(basePath, "a.txt");
        CreateDir(basePath, "z-folder");

        var user = new TestUser().Allow("z-folder");
        var result = BrowsingEndpoints.GetEntries(basePath, string.Empty, user, Page, PageSize);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("z-folder");
        await Assert.That(paginated.Items[0].Type).IsEqualTo(EntryType.Folder);
        await Assert.That(paginated.Items[1].Name).IsEqualTo("a.txt");
        await Assert.That(paginated.Items[1].Type).IsEqualTo(EntryType.File);
    }

    [Test]
    public async Task GetEntries_SortsAlphabeticallyWithinType()
    {
        using var _ = new DisposableTempDir(out var basePath);
        CreateDir(basePath, "b-folder");
        CreateDir(basePath, "a-folder");
        CreateFile(basePath, "z-file.txt");
        CreateFile(basePath, "a-file.txt");

        var user = new TestUser().Allow("a-folder").Allow("b-folder");
        var result = BrowsingEndpoints.GetEntries(basePath, string.Empty, user, Page, PageSize);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("a-folder");
        await Assert.That(paginated.Items[1].Name).IsEqualTo("b-folder");
        await Assert.That(paginated.Items[2].Name).IsEqualTo("a-file.txt");
        await Assert.That(paginated.Items[3].Name).IsEqualTo("z-file.txt");
    }

    [Test]
    public async Task GetEntries_Pagination_ReturnsRequestedPage()
    {
        using var _ = new DisposableTempDir(out var basePath);
        for (var i = 1; i <= 5; i++)
        {
            CreateFile(basePath, $"{i}.txt");
        }

        var user = new TestUser();

        var page1 = GetResult(BrowsingEndpoints.GetEntries(basePath, string.Empty, user, page: 1, pageSize: 2));
        await Assert.That(page1.Items.Count).IsEqualTo(2);
        await Assert.That(page1.TotalCount).IsEqualTo(5);
        await Assert.That(page1.Page).IsEqualTo(1);
        await Assert.That(page1.Items[0].Name).IsEqualTo("1.txt");
        await Assert.That(page1.Items[1].Name).IsEqualTo("2.txt");

        var page2 = GetResult(BrowsingEndpoints.GetEntries(basePath, string.Empty, user, page: 2, pageSize: 2));
        await Assert.That(page2.Items.Count).IsEqualTo(2);
        await Assert.That(page2.Page).IsEqualTo(2);
        await Assert.That(page2.Items[0].Name).IsEqualTo("3.txt");
        await Assert.That(page2.Items[1].Name).IsEqualTo("4.txt");

        var page3 = GetResult(BrowsingEndpoints.GetEntries(basePath, string.Empty, user, page: 3, pageSize: 2));
        await Assert.That(page3.Items.Count).IsEqualTo(1);
        await Assert.That(page3.Page).IsEqualTo(3);
        await Assert.That(page3.Items[0].Name).IsEqualTo("5.txt");
    }

    [Test]
    public async Task GetEntries_PageBeyondTotal_ReturnsEmptyItems()
    {
        using var _ = new DisposableTempDir(out var basePath);
        CreateFile(basePath, "only.txt");

        var user = new TestUser();
        var result = BrowsingEndpoints.GetEntries(basePath, string.Empty, user, page: 5, pageSize: 10);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items.Count).IsEqualTo(0);
        await Assert.That(paginated.TotalCount).IsEqualTo(1);
        await Assert.That(paginated.Page).IsEqualTo(5);
    }

    private static bool IsStatusCode(IResult result, int statusCode)
    {
        return result switch
        {
            Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult => statusCode == 401,
            Microsoft.AspNetCore.Http.HttpResults.NotFound => statusCode == 404,
            Microsoft.AspNetCore.Http.HttpResults.BadRequest => statusCode == 400,
            Microsoft.AspNetCore.Http.HttpResults.Ok<PaginatedResult<FolderEntry>> => statusCode == 200,
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
