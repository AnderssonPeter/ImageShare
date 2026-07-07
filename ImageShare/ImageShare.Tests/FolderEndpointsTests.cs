using ImageShare.Authentication;
using ImageShare.Browsing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
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
        (PaginatedResult<FolderEntry>)((Ok<PaginatedResult<FolderEntry>>)Unwrap(result)).Value!;

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
        await Assert.That(paginated.Items.Count).IsEqualTo(1);
        await Assert.That(paginated.TotalCount).IsEqualTo(1);
        await Assert.That(paginated.Page).IsEqualTo(Page);
        await Assert.That(paginated.PageSize).IsEqualTo(PageSize);

        var folder = paginated.Items.Single(e => e.Name == "allowed-folder");
        await Assert.That(folder.Type).IsEqualTo(EntryType.Folder);

        await Assert.That(paginated.Items.Any(e => e.Name == "blocked-folder")).IsFalse();
    }

    [Test]
    public async Task GetEntries_Root_ExcludesFiles()
    {
        var fs = new InMemoryFileProvider();
        AddFile(fs, "photo.jpg");
        AddFile(fs, "document.pdf");
        AddDir(fs, "images");

        var user = new TestUser().Allow("images");
        var paginated = GetResult(BrowsingEndpoints.GetEntries(fs, string.Empty, user, Page, PageSize));

        await Assert.That(paginated.TotalCount).IsEqualTo(1);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("images");
        await Assert.That(paginated.Items[0].Type).IsEqualTo(EntryType.Folder);
    }

    [Test]
    public async Task GetEntries_Root_AllFoldersBlocked_ReturnsEmpty()
    {
        var fs = new InMemoryFileProvider();
        AddDir(fs, "secret");
        AddFile(fs, "public.txt");

        var user = new TestUser();
        var result = BrowsingEndpoints.GetEntries(fs, string.Empty, user, Page, PageSize);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items.Count).IsEqualTo(0);
        await Assert.That(paginated.TotalCount).IsEqualTo(0);
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
        AddFile(fs, "sub/a.txt");
        AddDir(fs, "sub/z-folder");

        var user = new TestUser().Allow("sub");
        var result = BrowsingEndpoints.GetEntries(fs, "sub", user, Page, PageSize);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("z-folder");
        await Assert.That(paginated.Items[0].Type).IsEqualTo(EntryType.Folder);
        await Assert.That(paginated.Items[1].Name).IsEqualTo("a");
        await Assert.That(paginated.Items[1].Type).IsEqualTo(EntryType.File);
    }

    [Test]
    public async Task GetEntries_SortsAlphabeticallyWithinType()
    {
        var fs = new InMemoryFileProvider();
        AddDir(fs, "sub/b-folder");
        AddDir(fs, "sub/a-folder");
        AddFile(fs, "sub/z-file.txt");
        AddFile(fs, "sub/a-file.txt");

        var user = new TestUser().Allow("sub");
        var result = BrowsingEndpoints.GetEntries(fs, "sub", user, Page, PageSize);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("a-folder");
        await Assert.That(paginated.Items[1].Name).IsEqualTo("b-folder");
        await Assert.That(paginated.Items[2].Name).IsEqualTo("a-file");
        await Assert.That(paginated.Items[3].Name).IsEqualTo("z-file");
    }

    [Test]
    public async Task GetEntries_Subfolder_StripsFileExtensions()
    {
        var fs = new InMemoryFileProvider();
        AddFile(fs, "sub/image.avif");
        AddFile(fs, "sub/readme.txt");

        var user = new TestUser().Allow("sub");
        var paginated = GetResult(BrowsingEndpoints.GetEntries(fs, "sub", user, Page, PageSize));

        await Assert.That(paginated.TotalCount).IsEqualTo(2);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("image");
        await Assert.That(paginated.Items[0].Type).IsEqualTo(EntryType.File);
        await Assert.That(paginated.Items[1].Name).IsEqualTo("readme");
    }

    [Test]
    public async Task GetEntries_DeduplicatesSameNameDifferentFormats()
    {
        var fs = new InMemoryFileProvider();
        AddFile(fs, "sub/photo.jpg");
        AddFile(fs, "sub/photo.avif");
        AddFile(fs, "sub/photo.png");
        AddFile(fs, "sub/other.webp");

        var user = new TestUser().Allow("sub");
        var paginated = GetResult(BrowsingEndpoints.GetEntries(fs, "sub", user, Page, PageSize));

        await Assert.That(paginated.TotalCount).IsEqualTo(2);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("other");
        await Assert.That(paginated.Items[1].Name).IsEqualTo("photo");
    }

    [Test]
    public async Task GetEntries_Pagination_ReturnsRequestedPage()
    {
        var fs = new InMemoryFileProvider();
        for (var i = 1; i <= 5; i++)
        {
            AddFile(fs, $"sub/{i}.txt");
        }

        var user = new TestUser().Allow("sub");

        var page1 = GetResult(BrowsingEndpoints.GetEntries(fs, "sub", user, page: 1, pageSize: 2));
        await Assert.That(page1.Items.Count).IsEqualTo(2);
        await Assert.That(page1.TotalCount).IsEqualTo(5);
        await Assert.That(page1.Page).IsEqualTo(1);
        await Assert.That(page1.Items[0].Name).IsEqualTo("1");
        await Assert.That(page1.Items[1].Name).IsEqualTo("2");

        var page2 = GetResult(BrowsingEndpoints.GetEntries(fs, "sub", user, page: 2, pageSize: 2));
        await Assert.That(page2.Items.Count).IsEqualTo(2);
        await Assert.That(page2.Page).IsEqualTo(2);
        await Assert.That(page2.Items[0].Name).IsEqualTo("3");
        await Assert.That(page2.Items[1].Name).IsEqualTo("4");

        var page3 = GetResult(BrowsingEndpoints.GetEntries(fs, "sub", user, page: 3, pageSize: 2));
        await Assert.That(page3.Items.Count).IsEqualTo(1);
        await Assert.That(page3.Page).IsEqualTo(3);
        await Assert.That(page3.Items[0].Name).IsEqualTo("5");
    }

    [Test]
    public async Task GetEntries_PageBeyondTotal_ReturnsEmptyItems()
    {
        var fs = new InMemoryFileProvider();
        AddFile(fs, "sub/only.txt");

        var user = new TestUser().Allow("sub");
        var result = BrowsingEndpoints.GetEntries(fs, "sub", user, page: 5, pageSize: 10);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items.Count).IsEqualTo(0);
        await Assert.That(paginated.TotalCount).IsEqualTo(1);
        await Assert.That(paginated.Page).IsEqualTo(5);
    }

    [Test]
    public async Task GetEntries_ExcludesThumbprintFiles()
    {
        var fs = new InMemoryFileProvider();
        AddFile(fs, "sub/photo.avif");
        AddFile(fs, "sub/photo.thumb.jpg");
        AddFile(fs, "sub/image.png");
        AddFile(fs, "sub/image.thumb.png");

        var user = new TestUser().Allow("sub");
        var paginated = GetResult(BrowsingEndpoints.GetEntries(fs, "sub", user, Page, PageSize));

        await Assert.That(paginated.TotalCount).IsEqualTo(2);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("image");
        await Assert.That(paginated.Items[1].Name).IsEqualTo("photo");
    }

    private static bool IsStatusCode(IResult result, int statusCode)
    {
        return Unwrap(result) switch
        {
            UnauthorizedHttpResult => statusCode == 401,
            NotFound => statusCode == 404,
            BadRequest => statusCode == 400,
            Ok<PaginatedResult<FolderEntry>> => statusCode == 200,
            _ => false,
        };
    }

    private static IResult Unwrap(IResult result) =>
        result is INestedHttpResult nested ? (IResult)nested.Result : result;
}
