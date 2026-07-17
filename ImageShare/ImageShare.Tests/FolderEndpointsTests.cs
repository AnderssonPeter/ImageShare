using ImageMagick;
using ImageShare.Authentication;
using ImageShare.Browsing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Mirality.FileProviders;

namespace ImageShare.Tests;

[MicrosoftDI]
public class FolderEndpointsTests(ISyncWritableFileProvider fileProvider, IContentTypeProvider contentTypeProvider, IOptions<ImageFormatOptions> imageFormats)
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

    private void AddDirectory(string path) =>
        fileProvider.Write($"{path}/.keep", []);

    private void AddFile(string path, byte[] content) =>
        fileProvider.Write(path, content);
    private void AddFile(string path) =>
        AddFile(path, []);

    private void AddImageFile(string path, MagickFormat format)
    {
        using var image = new MagickImage(MagickColors.DodgerBlue, 100, 100);
        image.Format = format;
        AddFile(path, image.ToByteArray());
    }

    private static FileStreamHttpResult GetFileResult(IResult result) =>
        (FileStreamHttpResult)((INestedHttpResult)result).Result;

    [Test]
    public async Task GetEntries_Unauthenticated_ReturnsUnauthorized()
    {
        var user = new TestUser { IsAuthenticated = false };
        var result = BrowsingEndpoints.GetEntries(fileProvider, string.Empty, user, Page, PageSize);

        await Assert.That(IsStatusCode(result, 401)).IsTrue();
    }

    [Test]
    public async Task GetEntries_PathTraversal_ReturnsBadRequest()
    {
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
        var user = new TestUser();
        var result = BrowsingEndpoints.GetEntries(fileProvider, string.Empty, user, page, pageSize);

        await Assert.That(IsStatusCode(result, 400)).IsTrue();
    }

    [Test]
    public async Task GetEntries_Root_FiltersFoldersByAccess()
    {
        AddDirectory("allowed-folder");
        AddDirectory("blocked-folder");
        AddFile("file.txt");

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
        AddFile("photo.jpg");
        AddFile("document.pdf");
        AddDirectory("images");

        var user = new TestUser().Allow("images");
        var paginated = GetResult(BrowsingEndpoints.GetEntries(fileProvider, string.Empty, user, Page, PageSize));

        await Assert.That(paginated.TotalCount).IsEqualTo(1);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("images");
        await Assert.That(paginated.Items[0].Type).IsEqualTo(EntryType.Folder);
    }

    [Test]
    public async Task GetEntries_Root_AllFoldersBlocked_ReturnsEmpty()
    {
        AddDirectory("secret");
        AddFile("public.txt");

        var user = new TestUser();
        var result = BrowsingEndpoints.GetEntries(fileProvider, string.Empty, user, Page, PageSize);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items.Count).IsEqualTo(0);
        await Assert.That(paginated.TotalCount).IsEqualTo(0);
    }

    [Test]
    public async Task GetEntries_BlockedSubfolder_ReturnsNotFound()
    {
        AddDirectory("secret/nested");

        var user = new TestUser();
        var result = BrowsingEndpoints.GetEntries(fileProvider, "secret/nested", user, Page, PageSize);

        await Assert.That(IsStatusCode(result, 404)).IsTrue();
    }

    [Test]
    public async Task GetEntries_Subfolder_DoesNotFilterByAccess()
    {
        AddFile("allowed/sub-file.txt");
        AddFile("allowed/sub-secret/x");
        AddFile("allowed/sub-public/x");

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
        var user = new TestUser();
        var result = BrowsingEndpoints.GetEntries(fileProvider, string.Empty, user, Page, PageSize);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items.Count).IsEqualTo(0);
        await Assert.That(paginated.TotalCount).IsEqualTo(0);
    }

    [Test]
    public async Task GetEntries_EmptyFolder_ExcludedFromListing()
    {
        AddDirectory("empty-folder");
        AddDirectory("populated-folder");
        AddFile("populated-folder/file.png");

        var user = new TestUser().Allow("populated-folder").Allow("empty-folder");
        var result = BrowsingEndpoints.GetEntries(fileProvider, string.Empty, user, Page, PageSize);

        var paginated = GetResult(result);
        await Assert.That(paginated.Items.Count).IsEqualTo(1);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("populated-folder");
    }

    [Test]
    public async Task GetEntries_FolderWithOnlyThumbprintFiles_Excluded()
    {
        AddDirectory("normal-folder");
        AddFile("thumb-only-folder/photo.thumb.jpg");

        var user = new TestUser().Allow("normal-folder").Allow("thumb-only-folder");
        var paginated = GetResult(BrowsingEndpoints.GetEntries(fileProvider, string.Empty, user, Page, PageSize));

        await Assert.That(paginated.Items.Count).IsEqualTo(1);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("normal-folder");
    }

    [Test]
    public async Task GetEntries_Subfolder_EmptyDirectory_Hidden()
    {
        AddDirectory("parent/visible-folder");
        AddFile("parent/visible-folder/file.jpg");
        AddDirectory("parent/empty-folder");

        var user = new TestUser().Allow("parent");
        var paginated = GetResult(BrowsingEndpoints.GetEntries(fileProvider, "parent", user, Page, PageSize));

        await Assert.That(paginated.Items.Count).IsEqualTo(1);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("visible-folder");
    }

    [Test]
    public async Task GetEntries_SortsFoldersBeforeFiles()
    {
        AddFile("sub/a.txt");
        AddDirectory("sub/z-folder");

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
        AddDirectory("sub/b-folder");
        AddDirectory("sub/a-folder");
        AddFile("sub/z-file.txt");
        AddFile("sub/a-file.txt");

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
        AddFile("sub/image.avif");
        AddFile("sub/readme.txt");

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
        AddFile("sub/photo.jpg");
        AddFile("sub/photo.avif");
        AddFile("sub/photo.png");
        AddFile("sub/other.webp");

        var user = new TestUser().Allow("sub");
        var paginated = GetResult(BrowsingEndpoints.GetEntries(fileProvider, "sub", user, Page, PageSize));

        await Assert.That(paginated.TotalCount).IsEqualTo(2);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("other");
        await Assert.That(paginated.Items[1].Name).IsEqualTo("photo");
    }

    [Test]
    public async Task GetEntries_Pagination_ReturnsRequestedPage()
    {
        for (var i = 1; i <= 5; i++)
        {
            AddFile($"sub/{i}.txt");
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
        AddFile("sub/only.txt");

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
        AddFile("sub/photo.avif");
        AddFile("sub/photo.thumb.jpg");
        AddFile("sub/image.png");
        AddFile("sub/image.thumb.png");

        var user = new TestUser().Allow("sub");
        var paginated = GetResult(BrowsingEndpoints.GetEntries(fileProvider, "sub", user, Page, PageSize));

        await Assert.That(paginated.TotalCount).IsEqualTo(2);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("image");
        await Assert.That(paginated.Items[1].Name).IsEqualTo("photo");
    }

    [Test]
    public async Task GetRandomThumbnail_Unauthenticated_ReturnsUnauthorized()
    {
        var user = new TestUser { IsAuthenticated = false };
        var result = ImageEndpoints.GetRandomThumbnail(fileProvider, imageFormats.Value, contentTypeProvider, user, "photos", "");

        await Assert.That(IsStatusCode(result, 401)).IsTrue();
    }

    [Test]
    public async Task GetRandomThumbnail_PathTraversal_ReturnsBadRequest()
    {
        var user = new TestUser();
        var result = ImageEndpoints.GetRandomThumbnail(fileProvider, imageFormats.Value, contentTypeProvider, user, "../etc", "");

        await Assert.That(IsStatusCode(result, 400)).IsTrue();
    }

    [Test]
    public async Task GetRandomThumbnail_BlockedFolder_ReturnsForbidden()
    {
        AddImageFile("secret/photo.avif", MagickFormat.Avif);
        var user = new TestUser();
        var result = ImageEndpoints.GetRandomThumbnail(fileProvider, imageFormats.Value, contentTypeProvider, user, "secret", "");

        await Assert.That(IsStatusCode(result, 403)).IsTrue();
    }

    [Test]
    public async Task GetRandomThumbnail_NoImageFiles_ReturnsNotFound()
    {
        AddFile("empty/readme.txt");
        var user = new TestUser().Allow("empty");
        var result = ImageEndpoints.GetRandomThumbnail(fileProvider, imageFormats.Value, contentTypeProvider, user, "empty", "");

        await Assert.That(IsStatusCode(result, 404)).IsTrue();
    }

    [Test]
    public async Task GetRandomThumbnail_ReturnsThumbnail()
    {
        AddImageFile("vacation/photo.avif", MagickFormat.Avif);
        AddFile("vacation/photo.thumb.jpg", CreateThumbnail());
        var user = new TestUser().Allow("vacation");
        var result = ImageEndpoints.GetRandomThumbnail(fileProvider, imageFormats.Value, contentTypeProvider, user, "vacation", "");

        await Assert.That(IsStatusCode(result, 200)).IsTrue();
        var fileResult = GetFileResult(result);
        await Assert.That(fileResult.ContentType).IsEqualTo("image/jpeg");
    }

    [Test]
    public async Task GetRandomThumbnail_PicksRandomly()
    {
        AddImageFile("vacation/a.avif", MagickFormat.Avif);
        AddImageFile("vacation/b.jpg", MagickFormat.Jpeg);
        AddFile("vacation/a.thumb.jpg", CreateThumbnail());
        AddFile("vacation/b.thumb.jpg", CreateThumbnail());
        var user = new TestUser().Allow("vacation");

        var gotA = false;
        var gotB = false;
        for (var i = 0; i < 50; i++)
        {
            var result = ImageEndpoints.GetRandomThumbnail(fileProvider, imageFormats.Value, contentTypeProvider, user, "vacation", "");
            await Assert.That(IsStatusCode(result, 200)).IsTrue();
            var fileResult = GetFileResult(result);
            if (fileResult.FileDownloadName == null)
            {
                gotA = true;
            }
            else
            {
                gotB = true;
            }

            if (gotA && gotB)
            {
                break;
            }
        }

        await Assert.That(gotA).IsTrue();
        await Assert.That(gotB).IsTrue();
    }

    [Test]
    public async Task GetRandomThumbnail_NoThumbnailsAvailable_ReturnsNotFound()
    {
        AddImageFile("vacation/photo.avif", MagickFormat.Avif);
        var user = new TestUser().Allow("vacation");
        var result = ImageEndpoints.GetRandomThumbnail(fileProvider, imageFormats.Value, contentTypeProvider, user, "vacation", "");

        await Assert.That(IsStatusCode(result, 404)).IsTrue();
    }

    private static byte[] CreateThumbnail()
    {
        using var image = new MagickImage(MagickColors.DodgerBlue, 50, 50);
        image.Format = MagickFormat.Jpeg;
        return image.ToByteArray();
    }

    private static bool IsStatusCode(IResult result, int statusCode)
    {
        return Unwrap(result) switch
        {
            UnauthorizedHttpResult => statusCode == 401,
            NotFound => statusCode == 404,
            BadRequest => statusCode == 400,
            ForbidHttpResult => statusCode == 403,
            StatusCodeHttpResult => statusCode == 406,
            FileStreamHttpResult => statusCode == 200,
            Ok<PaginatedResult<FolderEntry>> => statusCode == 200,
            _ => false,
        };
    }

    private static IResult Unwrap(IResult result) =>
        result is INestedHttpResult nested ? (IResult)nested.Result : result;
}
