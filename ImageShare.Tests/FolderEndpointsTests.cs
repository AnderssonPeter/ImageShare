using ImageMagick;
using ImageShare.Browsing;
using Mediator;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Mirality.FileProviders;
using System.IO.Compression;

namespace ImageShare.Tests;

[MicrosoftDI]
public class FolderEndpointsTests(ISyncWritableFileProvider fileProvider, IMediator mediator, IOptions<ImageFormatOptions> imageFormats, TestUser user, TestImageFactory imageFactory)
{
    private const int Page = 1;
    private const int PageSize = 50;

    [Test]
    public async Task GetEntries_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        user.IsAuthenticated = false;

        // Act
        var result = await mediator.Send(new GetEntriesQuery("", Page, PageSize));

        // Assert
        await Assert.That(result.IsStatusCode(401)).IsTrue();
    }

    [Test]
    [Arguments("../etc")]
    [Arguments("/etc")]
    [Arguments("/")]
    [Arguments("/etc/passwd")]
    public async Task GetEntries_UnsafePath_ThrowsArgumentException(string path) =>
        await Assert.That(async () => await mediator.Send(new GetEntriesQuery(path, Page, PageSize))).Throws<ArgumentException>();

    [Test]
    [Arguments(0, 10)]
    [Arguments(1, 0)]
    [Arguments(1, 501)]
    public async Task GetEntries_InvalidPagination_ReturnsBadRequest(int page, int pageSize)
    {
        // Arrange
        // Act
        var result = await mediator.Send(new GetEntriesQuery("", page, pageSize));

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
        var result = await mediator.Send(new GetEntriesQuery("", Page, PageSize));

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
        var paginated = (await mediator.Send(new GetEntriesQuery("", Page, PageSize))).GetFolderEntriesResult();

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
        var result = await mediator.Send(new GetEntriesQuery("", Page, PageSize));

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
        var result = await mediator.Send(new GetEntriesQuery("secret/nested", Page, PageSize));

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
        var result = await mediator.Send(new GetEntriesQuery("allowed", Page, PageSize));

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
        var result = await mediator.Send(new GetEntriesQuery("", Page, PageSize));

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
        var result = await mediator.Send(new GetEntriesQuery("", Page, PageSize));

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
        var paginated = (await mediator.Send(new GetEntriesQuery("", Page, PageSize))).GetFolderEntriesResult();

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
        var paginated = (await mediator.Send(new GetEntriesQuery("parent", Page, PageSize))).GetFolderEntriesResult();

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
        var result = await mediator.Send(new GetEntriesQuery("sub", Page, PageSize));

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
        var result = await mediator.Send(new GetEntriesQuery("sub", Page, PageSize));

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
        fileProvider.AddFile("sub/readme.png");
        user.Allow("sub");

        // Act
        var paginated = (await mediator.Send(new GetEntriesQuery("sub", Page, PageSize))).GetFolderEntriesResult();

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
        var paginated = (await mediator.Send(new GetEntriesQuery("sub", Page, PageSize))).GetFolderEntriesResult();

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
            fileProvider.AddFile($"sub/{i}.png");
        }

        user.Allow("sub");

        // Act
        var page1 = (await mediator.Send(new GetEntriesQuery("sub", 1, 2))).GetFolderEntriesResult();
        var page2 = (await mediator.Send(new GetEntriesQuery("sub", 2, 2))).GetFolderEntriesResult();
        var page3 = (await mediator.Send(new GetEntriesQuery("sub", 3, 2))).GetFolderEntriesResult();

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
        fileProvider.AddFile("sub/only.png");
        user.Allow("sub");

        // Act
        var result = await mediator.Send(new GetEntriesQuery("sub", 5, 10));

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
        var paginated = (await mediator.Send(new GetEntriesQuery("sub", Page, PageSize))).GetFolderEntriesResult();

        // Assert
        await Assert.That(paginated.TotalCount).IsEqualTo(2);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("image");
        await Assert.That(paginated.Items[1].Name).IsEqualTo("photo");
    }

    [Test]
    public async Task GetEntries_ExcludesNonImageFiles()
    {
        // Arrange
        fileProvider.AddFile("sub/photo.avif");
        fileProvider.AddFile("sub/readme.txt");
        fileProvider.AddFile("sub/notes.md");
        user.Allow("sub");

        // Act
        var paginated = (await mediator.Send(new GetEntriesQuery("sub", Page, PageSize))).GetFolderEntriesResult();

        // Assert
        await Assert.That(paginated.TotalCount).IsEqualTo(1);
        await Assert.That(paginated.Items[0].Name).IsEqualTo("photo");
        await Assert.That(paginated.Items[0].Type).IsEqualTo(EntryType.File);
    }

    [Test]
    public async Task DownloadImages_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        user.IsAuthenticated = false;

        // Act
        var result = await mediator.Send(new DownloadImagesQuery(new StringValues(["vacation"]), new StringValues()));

        // Assert
        await Assert.That(result.IsStatusCode(401)).IsTrue();
    }

    [Test]
    public async Task DownloadImages_NoFolders_ReturnsBadRequest()
    {
        // Arrange
        // Act
        var result = await mediator.Send(new DownloadImagesQuery(new StringValues(), new StringValues()));

        // Assert
        await Assert.That(result.IsStatusCode(400)).IsTrue();
    }

    [Test]
    public async Task DownloadImages_BlockedFolder_ReturnsForbidden()
    {
        // Arrange
        fileProvider.AddFile("secret/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));

        // Act
        var result = await mediator.Send(new DownloadImagesQuery(new StringValues(["secret"]), new StringValues()));

        // Assert
        await Assert.That(result.IsStatusCode(403)).IsTrue();
    }

    [Test]
    public async Task DownloadImages_NoImages_ReturnsNotFound()
    {
        // Arrange
        fileProvider.AddFile("empty/readme.txt");
        user.Allow("empty");

        // Act
        var result = await mediator.Send(new DownloadImagesQuery(new StringValues(["empty"]), new StringValues()));

        // Assert
        await Assert.That(result.IsStatusCode(404)).IsTrue();
    }

    [Test]
    public async Task DownloadImages_ReturnsZipStream()
    {
        // Arrange
        fileProvider.AddFile("vacation/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new DownloadImagesQuery(new StringValues(["vacation"]), new StringValues()));

        // Assert
        await Assert.That(result.IsStatusCode(200)).IsTrue();
        await Assert.That(result.GetContentType()).IsEqualTo("application/zip");
    }

    [Test]
    public async Task DownloadImages_ZipContainsImagesRecursivelyExcludingThumbprintsAndNonImages()
    {
        // Arrange
        var photoA = imageFactory.CreateTestImage(MagickFormat.Avif);
        var photoB = imageFactory.CreateTestImage(MagickFormat.Jpeg);
        fileProvider.AddFile("album/photo.avif", photoA);
        fileProvider.AddFile("album/sub/nested.jpg", photoB);
        fileProvider.AddFile("album/sub/readme.txt", []);
        fileProvider.AddFile("album/photo.thumb.jpg", imageFactory.CreateThumbnail());
        user.Allow("album");

        var files = BrowsingHelpers.EnumerateImageFiles(fileProvider, imageFormats.Value, "album").ToList();

        // Act
        using var memory = new MemoryStream();
        await BrowsingHelpers.WriteZipAsync(files, memory, CancellationToken.None);
        memory.Position = 0;
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);

        // Assert
        var entries = archive.Entries.Select(entry => entry.FullName).ToList();
        await Assert.That(entries.Count).IsEqualTo(2);
        await Assert.That(entries).Contains("album/photo.avif");
        await Assert.That(entries).Contains("album/sub/nested.jpg");
        await Assert.That(entries.Any(name => name.Contains("thumb", StringComparison.OrdinalIgnoreCase))).IsFalse();
        await Assert.That(entries.Any(name => name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))).IsFalse();
    }

    [Test]
    public async Task DownloadImages_MultipleFolders_FlattensIntoSingleArchive()
    {
        // Arrange
        fileProvider.AddFile("album-a/a.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("album-b/b.jpg", imageFactory.CreateTestImage(MagickFormat.Jpeg));
        user.Allow("album-a").Allow("album-b");

        var files = BrowsingHelpers.EnumerateImageFiles(fileProvider, imageFormats.Value, "album-a")
            .Concat(BrowsingHelpers.EnumerateImageFiles(fileProvider, imageFormats.Value, "album-b"))
            .ToList();

        // Act
        using var memory = new MemoryStream();
        await BrowsingHelpers.WriteZipAsync(files, memory, CancellationToken.None);
        memory.Position = 0;
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);

        // Assert
        var entries = archive.Entries.Select(entry => entry.FullName).ToList();
        await Assert.That(entries.Count).IsEqualTo(2);
        await Assert.That(entries).Contains("album-a/a.avif");
        await Assert.That(entries).Contains("album-b/b.jpg");
    }

    [Test]
    public async Task DownloadImages_UnsupportedFormat_ReturnsBadRequest()
    {
        // Arrange
        fileProvider.AddFile("vacation/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new DownloadImagesQuery(new StringValues(["vacation"]), new StringValues(["gif"])));

        // Assert
        await Assert.That(result.IsStatusCode(400)).IsTrue();
    }

    [Test]
    public async Task DownloadImages_FormatFilter_OnlyIncludesMatchingExtension()
    {
        // Arrange
        fileProvider.AddFile("album/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("album/picture.jpg", imageFactory.CreateTestImage(MagickFormat.Jpeg));
        fileProvider.AddFile("album/drawing.png", imageFactory.CreateTestImage(MagickFormat.Png));
        user.Allow("album");

        var files = BrowsingHelpers.EnumerateImageFiles(fileProvider, imageFormats.Value, "album")
            .Where(file => string.Equals(Path.GetExtension(file.Info.Name).TrimStart('.'), "jpg", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Act
        using var memory = new MemoryStream();
        await BrowsingHelpers.WriteZipAsync(files, memory, CancellationToken.None);
        memory.Position = 0;
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);

        // Assert
        var entries = archive.Entries.Select(entry => entry.FullName).ToList();
        await Assert.That(entries.Count).IsEqualTo(1);
        await Assert.That(entries).Contains("album/picture.jpg");
    }

    [Test]
    public async Task DownloadImages_FormatFilter_NoMatches_ReturnsNotFound()
    {
        // Arrange
        fileProvider.AddFile("album/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        user.Allow("album");

        // Act
        var result = await mediator.Send(new DownloadImagesQuery(new StringValues(["album"]), new StringValues(["jpg"])));

        // Assert
        await Assert.That(result.IsStatusCode(404)).IsTrue();
    }

    [Test]
    public async Task DownloadImages_ZipEntriesUseNoCompression()
    {
        // Arrange
        var photo = imageFactory.CreateTestImage(MagickFormat.Avif);
        fileProvider.AddFile("vacation/photo.avif", photo);
        user.Allow("vacation");
        var files = BrowsingHelpers.EnumerateImageFiles(fileProvider, imageFormats.Value, "vacation").ToList();

        // Act
        using var memory = new MemoryStream();
        await BrowsingHelpers.WriteZipAsync(files, memory, CancellationToken.None);
        memory.Position = 0;
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);

        // Assert
        var entry = archive.Entries.Single();
        await Assert.That(entry.CompressedLength).IsEqualTo(entry.Length);
    }

    [Test]
    public async Task GetRandomImage_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        user.IsAuthenticated = false;

        // Act
        var result = await mediator.Send(new GetRandomImageQuery(new StringValues(["vacation"]), Accept: ""));

        // Assert
        await Assert.That(result.IsStatusCode(401)).IsTrue();
    }

    [Test]
    public async Task GetRandomImage_NoFolders_ReturnsBadRequest()
    {
        // Arrange
        // Act
        var result = await mediator.Send(new GetRandomImageQuery(new StringValues(), Accept: ""));

        // Assert
        await Assert.That(result.IsStatusCode(400)).IsTrue();
    }

    [Test]
    public async Task GetRandomImage_BlockedFolder_ReturnsForbidden()
    {
        // Arrange
        fileProvider.AddFile("secret/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));

        // Act
        var result = await mediator.Send(new GetRandomImageQuery(new StringValues(["secret"]), Accept: ""));

        // Assert
        await Assert.That(result.IsStatusCode(403)).IsTrue();
    }

    [Test]
    public async Task GetRandomImage_NoImages_ReturnsNotFound()
    {
        // Arrange
        fileProvider.AddFile("empty/readme.txt");
        user.Allow("empty");

        // Act
        var result = await mediator.Send(new GetRandomImageQuery(new StringValues(["empty"]), Accept: ""));

        // Assert
        await Assert.That(result.IsStatusCode(404)).IsTrue();
    }

    [Test]
    public async Task GetRandomImage_ReturnsImage()
    {
        // Arrange
        fileProvider.AddFile("vacation/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new GetRandomImageQuery(new StringValues(["vacation"]), Accept: ""));

        // Assert
        await Assert.That(result.IsStatusCode(200)).IsTrue();
        var fileResult = result.GetFileResult();
        await Assert.That(fileResult.ContentType).IsEqualTo("image/avif");
    }

    [Test]
    public async Task GetRandomImage_PicksRandomlyAcrossFoldersRecursively()
    {
        // Arrange
        fileProvider.AddFile("album-a/a.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("album-b/sub/b.jpg", imageFactory.CreateTestImage(MagickFormat.Jpeg));
        user.Allow("album-a").Allow("album-b");
        var gotA = false;
        var gotB = false;

        // Act
        for (var i = 0; i < 50; i++)
        {
            var result = await mediator.Send(new GetRandomImageQuery(new StringValues(["album-a", "album-b"]), Recursive: true));
            await Assert.That(result.IsStatusCode(200)).IsTrue();
            var fileResult = result.GetFileResult();
            var served = fileResult.FileStream.ReadAllBytes();
            if (served.SequenceEqual(fileProvider.GetFileInfo("album-a/a.avif").ReadAllBytes()))
            {
                gotA = true;
            }
            else if (served.SequenceEqual(fileProvider.GetFileInfo("album-b/sub/b.jpg").ReadAllBytes()))
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
    public async Task GetRandomImage_Thumbnail_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        user.IsAuthenticated = false;

        // Act
        var result = await mediator.Send(new GetRandomImageQuery(new StringValues(["photos"]), Thumbnail: true));

        // Assert
        await Assert.That(result.IsStatusCode(401)).IsTrue();
    }

    [Test]
    [Arguments("../etc")]
    [Arguments("/etc")]
    [Arguments("/")]
    [Arguments("/etc/passwd")]
    public async Task GetRandomImage_Thumbnail_UnsafePath_ThrowsArgumentException(string path) =>
        await Assert.That(async () => await mediator.Send(new GetRandomImageQuery(new StringValues([path]), Thumbnail: true))).Throws<ArgumentException>();

    [Test]
    public async Task GetRandomImage_Thumbnail_BlockedFolder_ReturnsForbidden()
    {
        // Arrange
        fileProvider.AddFile("secret/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));

        // Act
        var result = await mediator.Send(new GetRandomImageQuery(new StringValues(["secret"]), Thumbnail: true));

        // Assert
        await Assert.That(result.IsStatusCode(403)).IsTrue();
    }

    [Test]
    public async Task GetRandomImage_Thumbnail_NoImageFiles_ReturnsNotFound()
    {
        // Arrange
        fileProvider.AddFile("empty/readme.txt");
        user.Allow("empty");

        // Act
        var result = await mediator.Send(new GetRandomImageQuery(new StringValues(["empty"]), Thumbnail: true));

        // Assert
        await Assert.That(result.IsStatusCode(404)).IsTrue();
    }

    [Test]
    public async Task GetRandomImage_Thumbnail_ReturnsThumbnail()
    {
        // Arrange
        fileProvider.AddFile("vacation/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("vacation/photo.thumb.jpg", imageFactory.CreateThumbnail());
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new GetRandomImageQuery(new StringValues(["vacation"]), Thumbnail: true));

        // Assert
        await Assert.That(result.IsStatusCode(200)).IsTrue();
        var fileResult = result.GetFileResult();
        await Assert.That(fileResult.ContentType).IsEqualTo("image/jpeg");
    }

    [Test]
    public async Task GetRandomImage_Thumbnail_PicksRandomly()
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
            var result = await mediator.Send(new GetRandomImageQuery(new StringValues(["vacation"]), Thumbnail: true));
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
    public async Task GetRandomImage_Thumbnail_NoThumbnailsAvailable_ReturnsNotFound()
    {
        // Arrange
        fileProvider.AddFile("vacation/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new GetRandomImageQuery(new StringValues(["vacation"]), Thumbnail: true));

        // Assert
        await Assert.That(result.IsStatusCode(404)).IsTrue();
    }
}
