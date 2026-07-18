using ImageMagick;
using ImageShare.Browsing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Mirality.FileProviders;

namespace ImageShare.Tests;

[MicrosoftDI]
public class FolderEndpointsTests(ISyncWritableFileProvider fileProvider, IContentTypeProvider contentTypeProvider, IOptions<ImageFormatOptions> imageFormats, TestUser user)
{
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
        // Arrange
        user.IsAuthenticated = false;

        // Act
        var result = BrowsingEndpoints.GetEntries(fileProvider, string.Empty, user, Page, PageSize);

        // Assert
        await Assert.That(IsStatusCode(result, 401)).IsTrue();
    }

    [Test]
    public async Task GetEntries_PathTraversal_ThrowsArgumentException() =>
        await Assert.That(() => BrowsingEndpoints.GetEntries(fileProvider, "../etc", user, Page, PageSize)).Throws<ArgumentException>();

    [Test]
    [Arguments(0, 10)]
    [Arguments(1, 0)]
    [Arguments(1, 501)]
    public async Task GetEntries_InvalidPagination_ReturnsBadRequest(int page, int pageSize)
    {
        // Arrange
        // Act
        var result = BrowsingEndpoints.GetEntries(fileProvider, string.Empty, user, page, pageSize);

        // Assert
        await Assert.That(IsStatusCode(result, 400)).IsTrue();
    }

    [Test]
    public async Task GetEntries_Root_FiltersFoldersByAccess()
    {
        // Arrange
        AddDirectory("allowed-folder");
        AddFile("allowed-folder/real.txt");
        AddDirectory("blocked-folder");
        AddFile("file.txt");
        user.Allow("allowed-folder");

        // Act
        var result = BrowsingEndpoints.GetEntries(fileProvider, string.Empty, user, Page, PageSize);

        // Assert
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
        // Arrange
        AddFile("photo.jpg");
        AddFile("document.pdf");
        AddDirectory("images");
        AddFile("images/real.png");
        user.Allow("images");

        // Act
        var paginated = GetResult(BrowsingEndpoints.GetEntries(fileProvider, string.Empty, user, Page, PageSize));

        // Assert
        await Assert.That(paginated.TotalCount).IsEqualTo(1);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("images");
        await Assert.That(paginated.Items[0].Type).IsEqualTo(EntryType.Folder);
    }

    [Test]
    public async Task GetEntries_Root_AllFoldersBlocked_ReturnsEmpty()
    {
        // Arrange
        AddDirectory("secret");
        AddFile("public.txt");

        // Act
        var result = BrowsingEndpoints.GetEntries(fileProvider, string.Empty, user, Page, PageSize);

        // Assert
        var paginated = GetResult(result);
        await Assert.That(paginated.Items.Count).IsEqualTo(0);
        await Assert.That(paginated.TotalCount).IsEqualTo(0);
    }

    [Test]
    public async Task GetEntries_BlockedSubfolder_ReturnsNotFound()
    {
        // Arrange
        AddDirectory("secret/nested");

        // Act
        var result = BrowsingEndpoints.GetEntries(fileProvider, "secret/nested", user, Page, PageSize);

        // Assert
        await Assert.That(IsStatusCode(result, 404)).IsTrue();
    }

    [Test]
    public async Task GetEntries_Subfolder_DoesNotFilterByAccess()
    {
        // Arrange
        AddFile("allowed/sub-file.txt");
        AddFile("allowed/sub-secret/x");
        AddFile("allowed/sub-public/x");
        user.Allow("allowed");

        // Act
        var result = BrowsingEndpoints.GetEntries(fileProvider, "allowed", user, Page, PageSize);

        // Assert
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
        // Arrange
        // Act
        var result = BrowsingEndpoints.GetEntries(fileProvider, string.Empty, user, Page, PageSize);

        // Assert
        var paginated = GetResult(result);
        await Assert.That(paginated.Items.Count).IsEqualTo(0);
        await Assert.That(paginated.TotalCount).IsEqualTo(0);
    }

    [Test]
    public async Task GetEntries_EmptyFolder_ExcludedFromListing()
    {
        // Arrange
        AddDirectory("empty-folder");
        AddDirectory("populated-folder");
        AddFile("populated-folder/file.png");
        user.Allow("populated-folder").Allow("empty-folder");

        // Act
        var result = BrowsingEndpoints.GetEntries(fileProvider, string.Empty, user, Page, PageSize);

        // Assert
        var paginated = GetResult(result);
        await Assert.That(paginated.Items.Count).IsEqualTo(1);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("populated-folder");
    }

    [Test]
    public async Task GetEntries_FolderWithOnlyThumbprintFiles_Excluded()
    {
        // Arrange
        AddDirectory("normal-folder");
        AddFile("normal-folder/real.png");
        AddFile("thumb-only-folder/photo.thumb.jpg");
        user.Allow("normal-folder").Allow("thumb-only-folder");

        // Act
        var paginated = GetResult(BrowsingEndpoints.GetEntries(fileProvider, string.Empty, user, Page, PageSize));

        // Assert
        await Assert.That(paginated.Items.Count).IsEqualTo(1);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("normal-folder");
    }

    [Test]
    public async Task GetEntries_Subfolder_EmptyDirectory_Hidden()
    {
        // Arrange
        AddDirectory("parent/visible-folder");
        AddFile("parent/visible-folder/file.jpg");
        AddDirectory("parent/empty-folder");
        user.Allow("parent");

        // Act
        var paginated = GetResult(BrowsingEndpoints.GetEntries(fileProvider, "parent", user, Page, PageSize));

        // Assert
        await Assert.That(paginated.Items.Count).IsEqualTo(1);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("visible-folder");
    }

    [Test]
    public async Task GetEntries_SortsFoldersBeforeFiles()
    {
        // Arrange
        AddFile("sub/a.txt");
        AddDirectory("sub/z-folder");
        AddFile("sub/z-folder/real.txt");
        user.Allow("sub");

        // Act
        var result = BrowsingEndpoints.GetEntries(fileProvider, "sub", user, Page, PageSize);

        // Assert
        var paginated = GetResult(result);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("z-folder");
        await Assert.That(paginated.Items[0].Type).IsEqualTo(EntryType.Folder);
        await Assert.That(paginated.Items[1].Name).IsEqualTo("a");
        await Assert.That(paginated.Items[1].Type).IsEqualTo(EntryType.File);
    }

    [Test]
    public async Task GetEntries_SortsAlphabeticallyWithinType()
    {
        // Arrange
        AddDirectory("sub/b-folder");
        AddFile("sub/b-folder/real.txt");
        AddDirectory("sub/a-folder");
        AddFile("sub/a-folder/real.txt");
        AddFile("sub/z-file.txt");
        AddFile("sub/a-file.txt");
        user.Allow("sub");

        // Act
        var result = BrowsingEndpoints.GetEntries(fileProvider, "sub", user, Page, PageSize);

        // Assert
        var paginated = GetResult(result);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("a-folder");
        await Assert.That(paginated.Items[1].Name).IsEqualTo("b-folder");
        await Assert.That(paginated.Items[2].Name).IsEqualTo("a-file");
        await Assert.That(paginated.Items[3].Name).IsEqualTo("z-file");
    }

    [Test]
    public async Task GetEntries_Subfolder_StripsFileExtensions()
    {
        // Arrange
        AddFile("sub/image.avif");
        AddFile("sub/readme.txt");
        user.Allow("sub");

        // Act
        var paginated = GetResult(BrowsingEndpoints.GetEntries(fileProvider, "sub", user, Page, PageSize));

        // Assert
        await Assert.That(paginated.TotalCount).IsEqualTo(2);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("image");
        await Assert.That(paginated.Items[0].Type).IsEqualTo(EntryType.File);
        await Assert.That(paginated.Items[1].Name).IsEqualTo("readme");
    }

    [Test]
    public async Task GetEntries_DeduplicatesSameNameDifferentFormats()
    {
        // Arrange
        AddFile("sub/photo.jpg");
        AddFile("sub/photo.avif");
        AddFile("sub/photo.png");
        AddFile("sub/other.webp");
        user.Allow("sub");

        // Act
        var paginated = GetResult(BrowsingEndpoints.GetEntries(fileProvider, "sub", user, Page, PageSize));

        // Assert
        await Assert.That(paginated.TotalCount).IsEqualTo(2);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("other");
        await Assert.That(paginated.Items[1].Name).IsEqualTo("photo");
    }

    [Test]
    public async Task GetEntries_Pagination_ReturnsRequestedPage()
    {
        // Arrange
        for (var i = 1; i <= 5; i++)
        {
            AddFile($"sub/{i}.txt");
        }

        user.Allow("sub");

        // Act
        var page1 = GetResult(BrowsingEndpoints.GetEntries(fileProvider, "sub", user, page: 1, pageSize: 2));
        var page2 = GetResult(BrowsingEndpoints.GetEntries(fileProvider, "sub", user, page: 2, pageSize: 2));
        var page3 = GetResult(BrowsingEndpoints.GetEntries(fileProvider, "sub", user, page: 3, pageSize: 2));

        // Assert
        await Assert.That(page1.Items.Count).IsEqualTo(2);
        await Assert.That(page1.TotalCount).IsEqualTo(5);
        await Assert.That(page1.Page).IsEqualTo(1);
        await Assert.That(page1.Items[0].Name).IsEqualTo("1");
        await Assert.That(page1.Items[1].Name).IsEqualTo("2");

        await Assert.That(page2.Items.Count).IsEqualTo(2);
        await Assert.That(page2.Page).IsEqualTo(2);
        await Assert.That(page2.Items[0].Name).IsEqualTo("3");
        await Assert.That(page2.Items[1].Name).IsEqualTo("4");

        await Assert.That(page3.Items.Count).IsEqualTo(1);
        await Assert.That(page3.Page).IsEqualTo(3);
        await Assert.That(page3.Items[0].Name).IsEqualTo("5");
    }

    [Test]
    public async Task GetEntries_PageBeyondTotal_ReturnsEmptyItems()
    {
        // Arrange
        AddFile("sub/only.txt");
        user.Allow("sub");

        // Act
        var result = BrowsingEndpoints.GetEntries(fileProvider, "sub", user, page: 5, pageSize: 10);

        // Assert
        var paginated = GetResult(result);
        await Assert.That(paginated.Items.Count).IsEqualTo(0);
        await Assert.That(paginated.TotalCount).IsEqualTo(1);
        await Assert.That(paginated.Page).IsEqualTo(5);
    }

    [Test]
    public async Task GetEntries_ExcludesThumbprintFiles()
    {
        // Arrange
        AddFile("sub/photo.avif");
        AddFile("sub/photo.thumb.jpg");
        AddFile("sub/image.png");
        AddFile("sub/image.thumb.png");
        user.Allow("sub");

        // Act
        var paginated = GetResult(BrowsingEndpoints.GetEntries(fileProvider, "sub", user, Page, PageSize));

        // Assert
        await Assert.That(paginated.TotalCount).IsEqualTo(2);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("image");
        await Assert.That(paginated.Items[1].Name).IsEqualTo("photo");
    }

    [Test]
    public async Task GetRandomThumbnail_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        user.IsAuthenticated = false;

        // Act
        var result = ImageEndpoints.GetRandomThumbnail(fileProvider, imageFormats.Value, contentTypeProvider, user, "photos", "");

        // Assert
        await Assert.That(IsStatusCode(result, 401)).IsTrue();
    }

    [Test]
    public async Task GetRandomThumbnail_PathTraversal_ThrowsArgumentException() =>
        await Assert.That(() => ImageEndpoints.GetRandomThumbnail(fileProvider, imageFormats.Value, contentTypeProvider, user, "../etc", "")).Throws<ArgumentException>();

    [Test]
    public async Task GetRandomThumbnail_BlockedFolder_ReturnsForbidden()
    {
        // Arrange
        AddImageFile("secret/photo.avif", MagickFormat.Avif);

        // Act
        var result = ImageEndpoints.GetRandomThumbnail(fileProvider, imageFormats.Value, contentTypeProvider, user, "secret", "");

        // Assert
        await Assert.That(IsStatusCode(result, 403)).IsTrue();
    }

    [Test]
    public async Task GetRandomThumbnail_NoImageFiles_ReturnsNotFound()
    {
        // Arrange
        AddFile("empty/readme.txt");
        user.Allow("empty");

        // Act
        var result = ImageEndpoints.GetRandomThumbnail(fileProvider, imageFormats.Value, contentTypeProvider, user, "empty", "");

        // Assert
        await Assert.That(IsStatusCode(result, 404)).IsTrue();
    }

    [Test]
    public async Task GetRandomThumbnail_ReturnsThumbnail()
    {
        // Arrange
        AddImageFile("vacation/photo.avif", MagickFormat.Avif);
        AddFile("vacation/photo.thumb.jpg", CreateThumbnail());
        user.Allow("vacation");

        // Act
        var result = ImageEndpoints.GetRandomThumbnail(fileProvider, imageFormats.Value, contentTypeProvider, user, "vacation", "");

        // Assert
        await Assert.That(IsStatusCode(result, 200)).IsTrue();
        var fileResult = GetFileResult(result);
        await Assert.That(fileResult.ContentType).IsEqualTo("image/jpeg");
    }

    [Test]
    public async Task GetRandomThumbnail_PicksRandomly()
    {
        // Arrange
        AddImageFile("vacation/a.avif", MagickFormat.Avif);
        AddImageFile("vacation/b.jpg", MagickFormat.Jpeg);
        var thumbnailA = CreateThumbnail(MagickColors.DodgerBlue);
        var thumbnailB = CreateThumbnail(MagickColors.Crimson);
        AddFile("vacation/a.thumb.jpg", thumbnailA);
        AddFile("vacation/b.thumb.jpg", thumbnailB);
        user.Allow("vacation");
        var gotA = false;
        var gotB = false;

        // Act
        for (var i = 0; i < 50; i++)
        {
            var result = ImageEndpoints.GetRandomThumbnail(fileProvider, imageFormats.Value, contentTypeProvider, user, "vacation", "");
            await Assert.That(IsStatusCode(result, 200)).IsTrue();
            var fileResult = GetFileResult(result);
            var served = ReadAllBytes(fileResult.FileStream);
            if (served.SequenceEqual(thumbnailA))
            {
                gotA = true;
            }
            else if (served.SequenceEqual(thumbnailB))
            {
                gotB = true;
            }

            if (gotA && gotB)
            {
                break;
            }
        }

        // Assert
        await Assert.That(gotA).IsTrue();
        await Assert.That(gotB).IsTrue();
    }

    [Test]
    public async Task GetRandomThumbnail_NoThumbnailsAvailable_ReturnsNotFound()
    {
        // Arrange
        AddImageFile("vacation/photo.avif", MagickFormat.Avif);
        user.Allow("vacation");

        // Act
        var result = ImageEndpoints.GetRandomThumbnail(fileProvider, imageFormats.Value, contentTypeProvider, user, "vacation", "");

        // Assert
        await Assert.That(IsStatusCode(result, 404)).IsTrue();
    }

    private static byte[] CreateThumbnail() => CreateThumbnail(MagickColors.DodgerBlue);

    private static byte[] CreateThumbnail(IMagickColor<byte> color)
    {
        using var image = new MagickImage(color, 50, 50);
        image.Format = MagickFormat.Jpeg;
        return image.ToByteArray();
    }

    private static byte[] ReadAllBytes(Stream stream)
    {
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
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
