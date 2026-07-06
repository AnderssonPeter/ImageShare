using ImageShare.Authentication;
using ImageShare.Browsing;
using Microsoft.AspNetCore.Http;
using Mirality.FileProviders.InMemory;

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

    private static PaginatedResult<FolderEntry> GetResult(IResult result) =>
        (PaginatedResult<FolderEntry>)((Microsoft.AspNetCore.Http.HttpResults.Ok<PaginatedResult<FolderEntry>>)result).Value!;

    private const int Page = 1;
    private const int PageSize = 50;

    private static void AddDir(InMemoryFileProvider fs, string path) =>
        fs.Write($"{path}/.keep", Array.Empty<byte>());

    private static void AddFile(InMemoryFileProvider fs, string path) =>
        fs.Write(path, Array.Empty<byte>());

    [Test]
    public async Task GetEntries_Unauthenticated_ReturnsUnauthorized()
    {
        var fs = new InMemoryFileProvider();
        var user = new TestUser { IsAuthenticated = false };
        var result = BrowsingEndpoints.GetEntries(fs, string.Empty, user, Page, PageSize);

        await Assert.That(IsStatusCode(result, 401)).IsTrue();
    }

    [Test]
    public async Task GetEntries_PathTraversal_ReturnsBadRequest()
    {
        var fs = new InMemoryFileProvider();
        var user = new TestUser();
        var result = BrowsingEndpoints.GetEntries(fs, "../etc", user, Page, PageSize);

        await Assert.That(IsStatusCode(result, 400)).IsTrue();
    }

    [Test]
    [Arguments(0, 10)]
    [Arguments(1, 0)]
    [Arguments(1, 501)]
    public async Task GetEntries_InvalidPagination_ReturnsBadRequest(int page, int pageSize)
    {
        var fs = new InMemoryFileProvider();
        var user = new TestUser();
        var result = BrowsingEndpoints.GetEntries(fs, string.Empty, user, page, pageSize);

        await Assert.That(IsStatusCode(result, 400)).IsTrue();
    }

    [Test]
    public async Task GetEntries_Root_FiltersFoldersByAccess()
    {
        var fs = new InMemoryFileProvider();
        AddDir(fs, "allowed-folder");
        AddDir(fs, "blocked-folder");
        AddFile(fs, "file.txt");

        var user = new TestUser().Allow("allowed-folder");
        var result = BrowsingEndpoints.GetEntries(fs, string.Empty, user, Page, PageSize);

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
        var fs = new InMemoryFileProvider();
        AddDir(fs, "secret");
        AddFile(fs, "public.txt");

        var user = new TestUser();
        var result = BrowsingEndpoints.GetEntries(fs, string.Empty, user, Page, PageSize);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items.Count).IsEqualTo(1);
        await Assert.That(paginated.TotalCount).IsEqualTo(1);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("public.txt");
        await Assert.That(paginated.Items[0].Type).IsEqualTo(EntryType.File);
    }

    [Test]
    public async Task GetEntries_BlockedSubfolder_ReturnsNotFound()
    {
        var fs = new InMemoryFileProvider();
        AddDir(fs, "secret/nested");

        var user = new TestUser();
        var result = BrowsingEndpoints.GetEntries(fs, "secret/nested", user, Page, PageSize);

        await Assert.That(IsStatusCode(result, 404)).IsTrue();
    }

    [Test]
    public async Task GetEntries_Subfolder_DoesNotFilterByAccess()
    {
        var fs = new InMemoryFileProvider();
        AddFile(fs, "allowed/sub-file.txt");
        AddFile(fs, "allowed/sub-secret/x");
        AddFile(fs, "allowed/sub-public/x");

        var user = new TestUser().Allow("allowed");
        var result = BrowsingEndpoints.GetEntries(fs, "allowed", user, Page, PageSize);

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
        var fs = new InMemoryFileProvider();
        var user = new TestUser();
        var result = BrowsingEndpoints.GetEntries(fs, string.Empty, user, Page, PageSize);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items.Count).IsEqualTo(0);
        await Assert.That(paginated.TotalCount).IsEqualTo(0);
    }

    [Test]
    public async Task GetEntries_SortsFoldersBeforeFiles()
    {
        var fs = new InMemoryFileProvider();
        AddFile(fs, "a.txt");
        AddDir(fs, "z-folder");

        var user = new TestUser().Allow("z-folder");
        var result = BrowsingEndpoints.GetEntries(fs, string.Empty, user, Page, PageSize);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("z-folder");
        await Assert.That(paginated.Items[0].Type).IsEqualTo(EntryType.Folder);
        await Assert.That(paginated.Items[1].Name).IsEqualTo("a.txt");
        await Assert.That(paginated.Items[1].Type).IsEqualTo(EntryType.File);
    }

    [Test]
    public async Task GetEntries_SortsAlphabeticallyWithinType()
    {
        var fs = new InMemoryFileProvider();
        AddDir(fs, "b-folder");
        AddDir(fs, "a-folder");
        AddFile(fs, "z-file.txt");
        AddFile(fs, "a-file.txt");

        var user = new TestUser().Allow("a-folder").Allow("b-folder");
        var result = BrowsingEndpoints.GetEntries(fs, string.Empty, user, Page, PageSize);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("a-folder");
        await Assert.That(paginated.Items[1].Name).IsEqualTo("b-folder");
        await Assert.That(paginated.Items[2].Name).IsEqualTo("a-file.txt");
        await Assert.That(paginated.Items[3].Name).IsEqualTo("z-file.txt");
    }

    [Test]
    public async Task GetEntries_Pagination_ReturnsRequestedPage()
    {
        var fs = new InMemoryFileProvider();
        for (var i = 1; i <= 5; i++)
        {
            AddFile(fs, $"{i}.txt");
        }

        var user = new TestUser();

        var page1 = GetResult(BrowsingEndpoints.GetEntries(fs, string.Empty, user, page: 1, pageSize: 2));
        await Assert.That(page1.Items.Count).IsEqualTo(2);
        await Assert.That(page1.TotalCount).IsEqualTo(5);
        await Assert.That(page1.Page).IsEqualTo(1);
        await Assert.That(page1.Items[0].Name).IsEqualTo("1.txt");
        await Assert.That(page1.Items[1].Name).IsEqualTo("2.txt");

        var page2 = GetResult(BrowsingEndpoints.GetEntries(fs, string.Empty, user, page: 2, pageSize: 2));
        await Assert.That(page2.Items.Count).IsEqualTo(2);
        await Assert.That(page2.Page).IsEqualTo(2);
        await Assert.That(page2.Items[0].Name).IsEqualTo("3.txt");
        await Assert.That(page2.Items[1].Name).IsEqualTo("4.txt");

        var page3 = GetResult(BrowsingEndpoints.GetEntries(fs, string.Empty, user, page: 3, pageSize: 2));
        await Assert.That(page3.Items.Count).IsEqualTo(1);
        await Assert.That(page3.Page).IsEqualTo(3);
        await Assert.That(page3.Items[0].Name).IsEqualTo("5.txt");
    }

    [Test]
    public async Task GetEntries_PageBeyondTotal_ReturnsEmptyItems()
    {
        var fs = new InMemoryFileProvider();
        AddFile(fs, "only.txt");

        var user = new TestUser();
        var result = BrowsingEndpoints.GetEntries(fs, string.Empty, user, page: 5, pageSize: 10);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items.Count).IsEqualTo(0);
        await Assert.That(paginated.TotalCount).IsEqualTo(1);
        await Assert.That(paginated.Page).IsEqualTo(5);
    }

    [Test]
    public async Task GetEntries_ExcludesThumbprintFiles()
    {
        var fs = new InMemoryFileProvider();
        AddFile(fs, "photo.avif");
        AddFile(fs, "photo.thumb.jpg");
        AddFile(fs, "image.png");
        AddFile(fs, "image.thumb.png");

        var user = new TestUser();
        var paginated = GetResult(BrowsingEndpoints.GetEntries(fs, string.Empty, user, Page, PageSize));

        await Assert.That(paginated.TotalCount).IsEqualTo(2);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("image.png");
        await Assert.That(paginated.Items[1].Name).IsEqualTo("photo.avif");
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
