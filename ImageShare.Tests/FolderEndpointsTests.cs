using ImageMagick;
using ImageShare.Browsing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Mirality.FileProviders;

namespace ImageShare.Tests;

[MicrosoftDI]
public class FolderEndpointsTests(ISyncWritableFileProvider fileProvider, IContentTypeProvider contentTypeProvider, IOptions<ImageFormatOptions> imageFormats, TestUser user, TestImageFactory imageFactory)
{
    private const int Page = 1;
    private const int PageSize = 50;

    [Test]
    public async Task GetEntries_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        user.IsAuthenticated = false;

        // Act
        var result = BrowsingEndpoints.GetEntries(fileProvider, imageFormats.Value, string.Empty, user, Page, PageSize);

        // Assert
        await Assert.That(result.IsStatusCode(401)).IsTrue();
    }

    [Test]
    [Arguments("../etc")]
    [Arguments("/etc")]
    [Arguments("/")]
    [Arguments("/etc/passwd")]
    public async Task GetEntries_UnsafePath_ThrowsArgumentException(string path) =>
        await Assert.That(() => BrowsingEndpoints.GetEntries(fileProvider, imageFormats.Value, path, user, Page, PageSize)).Throws<ArgumentException>();

    [Test]
    [Arguments(0, 10)]
    [Arguments(1, 0)]
    [Arguments(1, 501)]
    public async Task GetEntries_InvalidPagination_ReturnsBadRequest(int page, int pageSize)
    {
        // Arrange
        // Act
        var result = BrowsingEndpoints.GetEntries(fileProvider, imageFormats.Value, string.Empty, user, page, pageSize);

        // Assert
        await Assert.That(result.IsStatusCode(400)).IsTrue();
    }

    [Test]
    public async Task GetEntries_Root_FiltersFoldersByAccess()
    {
        // Arrange
        fileProvider.AddDirectory("allowed-folder");
        fileProvider.AddFile("allowed-folder/real.png");
        fileProvider.AddDirectory("blocked-folder");
        fileProvider.AddFile("file.txt");
        user.Allow("allowed-folder");

        // Act
        var result = BrowsingEndpoints.GetEntries(fileProvider, imageFormats.Value, string.Empty, user, Page, PageSize);

        // Assert
        var paginated = result.GetFolderEntriesResult();
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
        fileProvider.AddFile("photo.jpg");
        fileProvider.AddFile("document.pdf");
        fileProvider.AddDirectory("images");
        fileProvider.AddFile("images/real.png");
        user.Allow("images");

        // Act
        var paginated = BrowsingEndpoints.GetEntries(fileProvider, imageFormats.Value, string.Empty, user, Page, PageSize).GetFolderEntriesResult();

        // Assert
        await Assert.That(paginated.TotalCount).IsEqualTo(1);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("images");
        await Assert.That(paginated.Items[0].Type).IsEqualTo(EntryType.Folder);
    }

    [Test]
    public async Task GetEntries_Root_AllFoldersBlocked_ReturnsEmpty()
    {
        // Arrange
        fileProvider.AddDirectory("secret");
        fileProvider.AddFile("public.txt");

        // Act
        var result = BrowsingEndpoints.GetEntries(fileProvider, imageFormats.Value, string.Empty, user, Page, PageSize);

        // Assert
        var paginated = result.GetFolderEntriesResult();
        await Assert.That(paginated.Items.Count).IsEqualTo(0);
        await Assert.That(paginated.TotalCount).IsEqualTo(0);
    }

    [Test]
    public async Task GetEntries_BlockedSubfolder_ReturnsNotFound()
    {
        // Arrange
        fileProvider.AddDirectory("secret/nested");

        // Act
        var result = BrowsingEndpoints.GetEntries(fileProvider, imageFormats.Value, "secret/nested", user, Page, PageSize);

        // Assert
        await Assert.That(result.IsStatusCode(404)).IsTrue();
    }

    [Test]
    public async Task GetEntries_Subfolder_DoesNotFilterByAccess()
    {
        // Arrange
        fileProvider.AddFile("allowed/sub-file.png");
        fileProvider.AddFile("allowed/sub-secret/real.png");
        fileProvider.AddFile("allowed/sub-public/real.png");
        user.Allow("allowed");

        // Act
        var result = BrowsingEndpoints.GetEntries(fileProvider, imageFormats.Value, "allowed", user, Page, PageSize);

        // Assert
        var paginated = result.GetFolderEntriesResult();
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
        var result = BrowsingEndpoints.GetEntries(fileProvider, imageFormats.Value, string.Empty, user, Page, PageSize);

        // Assert
        var paginated = result.GetFolderEntriesResult();
        await Assert.That(paginated.Items.Count).IsEqualTo(0);
        await Assert.That(paginated.TotalCount).IsEqualTo(0);
    }

    [Test]
    public async Task GetEntries_EmptyFolder_ExcludedFromListing()
    {
        // Arrange
        fileProvider.AddDirectory("empty-folder");
        fileProvider.AddDirectory("populated-folder");
        fileProvider.AddFile("populated-folder/file.png");
        user.Allow("populated-folder").Allow("empty-folder");

        // Act
        var result = BrowsingEndpoints.GetEntries(fileProvider, imageFormats.Value, string.Empty, user, Page, PageSize);

        // Assert
        var paginated = result.GetFolderEntriesResult();
        await Assert.That(paginated.Items.Count).IsEqualTo(1);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("populated-folder");
    }

    [Test]
    public async Task GetEntries_FolderWithOnlyThumbprintFiles_Excluded()
    {
        // Arrange
        fileProvider.AddDirectory("normal-folder");
        fileProvider.AddFile("normal-folder/real.png");
        fileProvider.AddFile("thumb-only-folder/photo.thumb.jpg");
        user.Allow("normal-folder").Allow("thumb-only-folder");

        // Act
        var paginated = BrowsingEndpoints.GetEntries(fileProvider, imageFormats.Value, string.Empty, user, Page, PageSize).GetFolderEntriesResult();

        // Assert
        await Assert.That(paginated.Items.Count).IsEqualTo(1);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("normal-folder");
    }

    [Test]
    public async Task GetEntries_Subfolder_EmptyDirectory_Hidden()
    {
        // Arrange
        fileProvider.AddDirectory("parent/visible-folder");
        fileProvider.AddFile("parent/visible-folder/file.jpg");
        fileProvider.AddDirectory("parent/empty-folder");
        user.Allow("parent");

        // Act
        var paginated = BrowsingEndpoints.GetEntries(fileProvider, imageFormats.Value, "parent", user, Page, PageSize).GetFolderEntriesResult();

        // Assert
        await Assert.That(paginated.Items.Count).IsEqualTo(1);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("visible-folder");
    }

    [Test]
    public async Task GetEntries_SortsFoldersBeforeFiles()
    {
        // Arrange
        fileProvider.AddFile("sub/a.png");
        fileProvider.AddDirectory("sub/z-folder");
        fileProvider.AddFile("sub/z-folder/real.png");
        user.Allow("sub");

        // Act
        var result = BrowsingEndpoints.GetEntries(fileProvider, imageFormats.Value, "sub", user, Page, PageSize);

        // Assert
        var paginated = result.GetFolderEntriesResult();
        await Assert.That(paginated.Items[0].Name).IsEqualTo("z-folder");
        await Assert.That(paginated.Items[0].Type).IsEqualTo(EntryType.Folder);
        await Assert.That(paginated.Items[1].Name).IsEqualTo("a");
        await Assert.That(paginated.Items[1].Type).IsEqualTo(EntryType.File);
    }

    [Test]
    public async Task GetEntries_SortsAlphabeticallyWithinType()
    {
        // Arrange
        fileProvider.AddDirectory("sub/b-folder");
        fileProvider.AddFile("sub/b-folder/real.png");
        fileProvider.AddDirectory("sub/a-folder");
        fileProvider.AddFile("sub/a-folder/real.png");
        fileProvider.AddFile("sub/z-file.png");
        fileProvider.AddFile("sub/a-file.png");
        user.Allow("sub");

        // Act
        var result = BrowsingEndpoints.GetEntries(fileProvider, imageFormats.Value, "sub", user, Page, PageSize);

        // Assert
        var paginated = result.GetFolderEntriesResult();
        await Assert.That(paginated.Items[0].Name).IsEqualTo("a-folder");
        await Assert.That(paginated.Items[1].Name).IsEqualTo("b-folder");
        await Assert.That(paginated.Items[2].Name).IsEqualTo("a-file");
        await Assert.That(paginated.Items[3].Name).IsEqualTo("z-file");
    }

    [Test]
    public async Task GetEntries_Subfolder_StripsFileExtensions()
    {
        // Arrange
        fileProvider.AddFile("sub/image.avif");
        fileProvider.AddFile("sub/readme.txt");
        user.Allow("sub");

        // Act
        var paginated = BrowsingEndpoints.GetEntries(fileProvider, imageFormats.Value, "sub", user, Page, PageSize).GetFolderEntriesResult();

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
        fileProvider.AddFile("sub/photo.jpg");
        fileProvider.AddFile("sub/photo.avif");
        fileProvider.AddFile("sub/photo.png");
        fileProvider.AddFile("sub/other.webp");
        user.Allow("sub");

        // Act
        var paginated = BrowsingEndpoints.GetEntries(fileProvider, imageFormats.Value, "sub", user, Page, PageSize).GetFolderEntriesResult();

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
            fileProvider.AddFile($"sub/{i}.txt");
        }

        user.Allow("sub");

        // Act
        var page1 = BrowsingEndpoints.GetEntries(fileProvider, imageFormats.Value, "sub", user, page: 1, pageSize: 2).GetFolderEntriesResult();
        var page2 = BrowsingEndpoints.GetEntries(fileProvider, imageFormats.Value, "sub", user, page: 2, pageSize: 2).GetFolderEntriesResult();
        var page3 = BrowsingEndpoints.GetEntries(fileProvider, imageFormats.Value, "sub", user, page: 3, pageSize: 2).GetFolderEntriesResult();

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
        fileProvider.AddFile("sub/only.txt");
        user.Allow("sub");

        // Act
        var result = BrowsingEndpoints.GetEntries(fileProvider, imageFormats.Value, "sub", user, page: 5, pageSize: 10);

        // Assert
        var paginated = result.GetFolderEntriesResult();
        await Assert.That(paginated.Items.Count).IsEqualTo(0);
        await Assert.That(paginated.TotalCount).IsEqualTo(1);
        await Assert.That(paginated.Page).IsEqualTo(5);
    }

    [Test]
    public async Task GetEntries_ExcludesThumbprintFiles()
    {
        // Arrange
        fileProvider.AddFile("sub/photo.avif");
        fileProvider.AddFile("sub/photo.thumb.jpg");
        fileProvider.AddFile("sub/image.png");
        fileProvider.AddFile("sub/image.thumb.png");
        user.Allow("sub");

        // Act
        var paginated = BrowsingEndpoints.GetEntries(fileProvider, imageFormats.Value, "sub", user, Page, PageSize).GetFolderEntriesResult();

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
        await Assert.That(result.IsStatusCode(401)).IsTrue();
    }

    [Test]
    [Arguments("../etc")]
    [Arguments("/etc")]
    [Arguments("/")]
    [Arguments("/etc/passwd")]
    public async Task GetRandomThumbnail_UnsafePath_ThrowsArgumentException(string path) =>
        await Assert.That(() => ImageEndpoints.GetRandomThumbnail(fileProvider, imageFormats.Value, contentTypeProvider, user, path, "")).Throws<ArgumentException>();

    [Test]
    public async Task GetRandomThumbnail_BlockedFolder_ReturnsForbidden()
    {
        // Arrange
        fileProvider.AddFile("secret/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));

        // Act
        var result = ImageEndpoints.GetRandomThumbnail(fileProvider, imageFormats.Value, contentTypeProvider, user, "secret", "");

        // Assert
        await Assert.That(result.IsStatusCode(403)).IsTrue();
    }

    [Test]
    public async Task GetRandomThumbnail_NoImageFiles_ReturnsNotFound()
    {
        // Arrange
        fileProvider.AddFile("empty/readme.txt");
        user.Allow("empty");

        // Act
        var result = ImageEndpoints.GetRandomThumbnail(fileProvider, imageFormats.Value, contentTypeProvider, user, "empty", "");

        // Assert
        await Assert.That(result.IsStatusCode(404)).IsTrue();
    }

    [Test]
    public async Task GetRandomThumbnail_ReturnsThumbnail()
    {
        // Arrange
        fileProvider.AddFile("vacation/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("vacation/photo.thumb.jpg", imageFactory.CreateThumbnail());
        user.Allow("vacation");

        // Act
        var result = ImageEndpoints.GetRandomThumbnail(fileProvider, imageFormats.Value, contentTypeProvider, user, "vacation", "");

        // Assert
        await Assert.That(result.IsStatusCode(200)).IsTrue();
        var fileResult = result.GetFileResult();
        await Assert.That(fileResult.ContentType).IsEqualTo("image/jpeg");
    }

    [Test]
    public async Task GetRandomThumbnail_PicksRandomly()
    {
        // Arrange
        fileProvider.AddFile("vacation/a.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("vacation/b.jpg", imageFactory.CreateTestImage(MagickFormat.Jpeg));
        var thumbnailA = imageFactory.CreateThumbnail(MagickColors.DodgerBlue);
        var thumbnailB = imageFactory.CreateThumbnail(MagickColors.Crimson);
        fileProvider.AddFile("vacation/a.thumb.jpg", thumbnailA);
        fileProvider.AddFile("vacation/b.thumb.jpg", thumbnailB);
        user.Allow("vacation");
        var gotA = false;
        var gotB = false;

        // Act
        for (var i = 0; i < 50; i++)
        {
            var result = ImageEndpoints.GetRandomThumbnail(fileProvider, imageFormats.Value, contentTypeProvider, user, "vacation", "");
            await Assert.That(result.IsStatusCode(200)).IsTrue();
            var fileResult = result.GetFileResult();
            var served = fileResult.FileStream.ReadAllBytes();
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
        fileProvider.AddFile("vacation/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        user.Allow("vacation");

        // Act
        var result = ImageEndpoints.GetRandomThumbnail(fileProvider, imageFormats.Value, contentTypeProvider, user, "vacation", "");

        // Assert
        await Assert.That(result.IsStatusCode(404)).IsTrue();
    }
}
