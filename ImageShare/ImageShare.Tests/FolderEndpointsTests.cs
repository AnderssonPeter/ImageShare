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
        ((Ok<PaginatedResult<FolderEntry>>)Unwrap(result)).Value!;

    private const int Page = 1;
    private const int PageSize = 50;

    private static void AddDir(InMemoryFileProvider fileProvider, string path) =>
        fileProvider.Write($"{path}/.keep", []);

    private static void AddFile(InMemoryFileProvider fileProvider, string path) =>
        fileProvider.Write(path, []);

    [Test]
    public async Task GetEntries_Unauthenticated_ReturnsUnauthorized()
    {
        var fileProvider = new InMemoryFileProvider();
        var user = new TestUser { IsAuthenticated = false };
        var result = BrowsingEndpoints.GetEntries(fileProvider, string.Empty, user, Page, PageSize);

        await Assert.That(IsStatusCode(result, 401)).IsTrue();
    }

    [Test]
    public async Task GetEntries_PathTraversal_ReturnsBadRequest()
    {
        var fileProvider = new InMemoryFileProvider();
        var user = new TestUser();
        var result = BrowsingEndpoints.GetEntries(fileProvider, "../etc", user, Page, PageSize);

        await Assert.That(IsStatusCode(result, 400)).IsTrue();
    }

    [Test]
    [Arguments(0, 10)]
    [Arguments(1, 0)]
    [Arguments(1, 501)]
    public async Task GetEntries_InvalidPagination_ReturnsBadRequest(int page, int pageSize)
    {
        var fileProvider = new InMemoryFileProvider();
        var user = new TestUser();
        var result = BrowsingEndpoints.GetEntries(fileProvider, string.Empty, user, page, pageSize);

        await Assert.That(IsStatusCode(result, 400)).IsTrue();
    }

    [Test]
    public async Task GetEntries_Root_FiltersFoldersByAccess()
    {
        var fileProvider = new InMemoryFileProvider();
        AddDir(fileProvider, "allowed-folder");
        AddDir(fileProvider, "blocked-folder");
        AddFile(fileProvider, "file.txt");

        var user = new TestUser().Allow("allowed-folder");
        var result = BrowsingEndpoints.GetEntries(fileProvider, string.Empty, user, Page, PageSize);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items).IsNotNull();
        await Assert.That(paginated.Items.Count).IsEqualTo(1);
        await Assert.That(paginated.TotalCount).IsEqualTo(1);
        await Assert.That(paginated.Page).IsEqualTo(Page);
        await Assert.That(paginated.PageSize).IsEqualTo(PageSize);

        var folder = paginated.Items.Single(entry => entry.Name == "allowed-folder");
        await Assert.That(folder.Type).IsEqualTo(EntryType.Folder);

        await Assert.That(paginated.Items.Any(entry => entry.Name == "blocked-folder")).IsFalse();
    }

    [Test]
    public async Task GetEntries_Root_ExcludesFiles()
    {
        var fileProvider = new InMemoryFileProvider();
        AddFile(fileProvider, "photo.jpg");
        AddFile(fileProvider, "document.pdf");
        AddDir(fileProvider, "images");

        var user = new TestUser().Allow("images");
        var paginated = GetResult(BrowsingEndpoints.GetEntries(fileProvider, string.Empty, user, Page, PageSize));

        await Assert.That(paginated.TotalCount).IsEqualTo(1);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("images");
        await Assert.That(paginated.Items[0].Type).IsEqualTo(EntryType.Folder);
    }

    [Test]
    public async Task GetEntries_Root_AllFoldersBlocked_ReturnsEmpty()
    {
        var fileProvider = new InMemoryFileProvider();
        AddDir(fileProvider, "secret");
        AddFile(fileProvider, "public.txt");

        var user = new TestUser();
        var result = BrowsingEndpoints.GetEntries(fileProvider, string.Empty, user, Page, PageSize);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items.Count).IsEqualTo(0);
        await Assert.That(paginated.TotalCount).IsEqualTo(0);
    }

    [Test]
    public async Task GetEntries_BlockedSubfolder_ReturnsNotFound()
    {
        var fileProvider = new InMemoryFileProvider();
        AddDir(fileProvider, "secret/nested");

        var user = new TestUser();
        var result = BrowsingEndpoints.GetEntries(fileProvider, "secret/nested", user, Page, PageSize);

        await Assert.That(IsStatusCode(result, 404)).IsTrue();
    }

    [Test]
    public async Task GetEntries_Subfolder_DoesNotFilterByAccess()
    {
        var fileProvider = new InMemoryFileProvider();
        AddFile(fileProvider, "allowed/sub-file.txt");
        AddFile(fileProvider, "allowed/sub-secret/x");
        AddFile(fileProvider, "allowed/sub-public/x");

        var user = new TestUser().Allow("allowed");
        var result = BrowsingEndpoints.GetEntries(fileProvider, "allowed", user, Page, PageSize);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items.Count).IsEqualTo(3);
        await Assert.That(paginated.TotalCount).IsEqualTo(3);

        var folder1 = paginated.Items.Single(entry => entry.Name == "sub-secret");
        await Assert.That(folder1.Type).IsEqualTo(EntryType.Folder);
        var folder2 = paginated.Items.Single(entry => entry.Name == "sub-public");
        await Assert.That(folder2.Type).IsEqualTo(EntryType.Folder);
    }

    [Test]
    public async Task GetEntries_EmptyDirectory_ReturnsEmpty()
    {
        var fileProvider = new InMemoryFileProvider();
        var user = new TestUser();
        var result = BrowsingEndpoints.GetEntries(fileProvider, string.Empty, user, Page, PageSize);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items.Count).IsEqualTo(0);
        await Assert.That(paginated.TotalCount).IsEqualTo(0);
    }

    [Test]
    public async Task GetEntries_SortsFoldersBeforeFiles()
    {
        var fileProvider = new InMemoryFileProvider();
        AddFile(fileProvider, "sub/a.txt");
        AddDir(fileProvider, "sub/z-folder");

        var user = new TestUser().Allow("sub");
        var result = BrowsingEndpoints.GetEntries(fileProvider, "sub", user, Page, PageSize);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("z-folder");
        await Assert.That(paginated.Items[0].Type).IsEqualTo(EntryType.Folder);
        await Assert.That(paginated.Items[1].Name).IsEqualTo("a");
        await Assert.That(paginated.Items[1].Type).IsEqualTo(EntryType.File);
    }

    [Test]
    public async Task GetEntries_SortsAlphabeticallyWithinType()
    {
        var fileProvider = new InMemoryFileProvider();
        AddDir(fileProvider, "sub/b-folder");
        AddDir(fileProvider, "sub/a-folder");
        AddFile(fileProvider, "sub/z-file.txt");
        AddFile(fileProvider, "sub/a-file.txt");

        var user = new TestUser().Allow("sub");
        var result = BrowsingEndpoints.GetEntries(fileProvider, "sub", user, Page, PageSize);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("a-folder");
        await Assert.That(paginated.Items[1].Name).IsEqualTo("b-folder");
        await Assert.That(paginated.Items[2].Name).IsEqualTo("a-file");
        await Assert.That(paginated.Items[3].Name).IsEqualTo("z-file");
    }

    [Test]
    public async Task GetEntries_Subfolder_StripsFileExtensions()
    {
        var fileProvider = new InMemoryFileProvider();
        AddFile(fileProvider, "sub/image.avif");
        AddFile(fileProvider, "sub/readme.txt");

        var user = new TestUser().Allow("sub");
        var paginated = GetResult(BrowsingEndpoints.GetEntries(fileProvider, "sub", user, Page, PageSize));

        await Assert.That(paginated.TotalCount).IsEqualTo(2);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("image");
        await Assert.That(paginated.Items[0].Type).IsEqualTo(EntryType.File);
        await Assert.That(paginated.Items[1].Name).IsEqualTo("readme");
    }

    [Test]
    public async Task GetEntries_DeduplicatesSameNameDifferentFormats()
    {
        var fileProvider = new InMemoryFileProvider();
        AddFile(fileProvider, "sub/photo.jpg");
        AddFile(fileProvider, "sub/photo.avif");
        AddFile(fileProvider, "sub/photo.png");
        AddFile(fileProvider, "sub/other.webp");

        var user = new TestUser().Allow("sub");
        var paginated = GetResult(BrowsingEndpoints.GetEntries(fileProvider, "sub", user, Page, PageSize));

        await Assert.That(paginated.TotalCount).IsEqualTo(2);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("other");
        await Assert.That(paginated.Items[1].Name).IsEqualTo("photo");
    }

    [Test]
    public async Task GetEntries_Pagination_ReturnsRequestedPage()
    {
        var fileProvider = new InMemoryFileProvider();
        for (var i = 1; i <= 5; i++)
        {
            AddFile(fileProvider, $"sub/{i}.txt");
        }

        var user = new TestUser().Allow("sub");

        var page1 = GetResult(BrowsingEndpoints.GetEntries(fileProvider, "sub", user, page: 1, pageSize: 2));
        await Assert.That(page1.Items.Count).IsEqualTo(2);
        await Assert.That(page1.TotalCount).IsEqualTo(5);
        await Assert.That(page1.Page).IsEqualTo(1);
        await Assert.That(page1.Items[0].Name).IsEqualTo("1");
        await Assert.That(page1.Items[1].Name).IsEqualTo("2");

        var page2 = GetResult(BrowsingEndpoints.GetEntries(fileProvider, "sub", user, page: 2, pageSize: 2));
        await Assert.That(page2.Items.Count).IsEqualTo(2);
        await Assert.That(page2.Page).IsEqualTo(2);
        await Assert.That(page2.Items[0].Name).IsEqualTo("3");
        await Assert.That(page2.Items[1].Name).IsEqualTo("4");

        var page3 = GetResult(BrowsingEndpoints.GetEntries(fileProvider, "sub", user, page: 3, pageSize: 2));
        await Assert.That(page3.Items.Count).IsEqualTo(1);
        await Assert.That(page3.Page).IsEqualTo(3);
        await Assert.That(page3.Items[0].Name).IsEqualTo("5");
    }

    [Test]
    public async Task GetEntries_PageBeyondTotal_ReturnsEmptyItems()
    {
        var fileProvider = new InMemoryFileProvider();
        AddFile(fileProvider, "sub/only.txt");

        var user = new TestUser().Allow("sub");
        var result = BrowsingEndpoints.GetEntries(fileProvider, "sub", user, page: 5, pageSize: 10);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items.Count).IsEqualTo(0);
        await Assert.That(paginated.TotalCount).IsEqualTo(1);
        await Assert.That(paginated.Page).IsEqualTo(5);
    }

    [Test]
    public async Task GetEntries_ExcludesThumbprintFiles()
    {
        var fileProvider = new InMemoryFileProvider();
        AddFile(fileProvider, "sub/photo.avif");
        AddFile(fileProvider, "sub/photo.thumb.jpg");
        AddFile(fileProvider, "sub/image.png");
        AddFile(fileProvider, "sub/image.thumb.png");

        var user = new TestUser().Allow("sub");
        var paginated = GetResult(BrowsingEndpoints.GetEntries(fileProvider, "sub", user, Page, PageSize));

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
