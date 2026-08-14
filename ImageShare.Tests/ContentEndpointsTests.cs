using System.IO.Compression;
using ImageMagick;
using ImageShare.Browsing;
using ImageShare.Errors;
using Mediator;
using Microsoft.Extensions.Primitives;
using Mirality.FileProviders;

namespace ImageShare.Tests;

[MicrosoftDI]
public class ContentEndpointsTests(ISyncWritableFileProvider fileProvider, IMediator mediator, ImageEnumerator imageEnumerator, TestUser user, TestUsageAgreement usageAgreement, TestImageFactory imageFactory)
{
    [Test]
    public async Task GetEntries_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        user.IsAuthenticated = false;

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new GetEntriesQuery(RelativePath.Root))).Throws<NotAuthenticatedException>();
    }

    [Test]
    [Arguments("../etc")]
    [Arguments("/etc")]
    [Arguments("/etc/passwd")]
    public async Task GetEntries_UnsafePath_ThrowsArgumentException(string path) =>
        await Assert.That(async () => await mediator.Send(new GetEntriesQuery(new RelativePath(path)))).Throws<ArgumentException>();

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
        var result = await mediator.Send(new GetEntriesQuery(RelativePath.Root));

        // Assert
        var entries = result.GetFolderEntriesResult();
        await Assert.That(entries).IsNotNull();
        await Assert.That(entries.Count).IsEqualTo(1);

        var folder = entries.Single(entry => entry.Name == "allowed-folder");
        await Assert.That(folder.Type).IsEqualTo(EntryType.Folder);
        await Assert.That(folder.Path).IsEqualTo("allowed-folder");

        await Assert.That(entries.Any(entry => entry.Name == "blocked-folder")).IsFalse();
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
        var entries = (await mediator.Send(new GetEntriesQuery(RelativePath.Root))).GetFolderEntriesResult();

        // Assert
        await Assert.That(entries.Count).IsEqualTo(1);
        await Assert.That(entries[0].Name).IsEqualTo("images");
        await Assert.That(entries[0].Path).IsEqualTo("images");
        await Assert.That(entries[0].Type).IsEqualTo(EntryType.Folder);
    }

    [Test]
    public async Task GetEntries_Root_AllFoldersBlocked_ReturnsEmpty()
    {
        // Arrange
        fileProvider.AddDirectory("secret");
        fileProvider.AddFile("public.txt");

        // Act
        var result = await mediator.Send(new GetEntriesQuery(RelativePath.Root));

        // Assert
        var entries = result.GetFolderEntriesResult();
        await Assert.That(entries.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetEntries_BlockedSubfolder_ReturnsNotFound()
    {
        // Arrange
        fileProvider.AddDirectory("secret/nested");

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new GetEntriesQuery(new RelativePath("secret/nested")))).Throws<NotFoundException>();
    }

    [Test]
    public async Task GetEntries_NonExistentDirectory_ReturnsNotFound()
    {
        // Arrange
        user.Allow("allowed");

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new GetEntriesQuery(new RelativePath("allowed/non-existent")))).Throws<NotFoundException>();
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
        var result = await mediator.Send(new GetEntriesQuery(new RelativePath("allowed")));

        // Assert
        var entries = result.GetFolderEntriesResult();
        await Assert.That(entries.Count).IsEqualTo(3);

        var file = entries.Single(entry => entry.Name == "sub-file");
        await Assert.That(file.Type).IsEqualTo(EntryType.File);
        await Assert.That(file.Path).IsEqualTo("allowed/sub-file");

        var folder1 = entries.Single(entry => entry.Name == "sub-secret");
        await Assert.That(folder1.Type).IsEqualTo(EntryType.Folder);
        await Assert.That(folder1.Path).IsEqualTo("allowed/sub-secret");
        var folder2 = entries.Single(entry => entry.Name == "sub-public");
        await Assert.That(folder2.Type).IsEqualTo(EntryType.Folder);
        await Assert.That(folder2.Path).IsEqualTo("allowed/sub-public");
    }

    [Test]
    public async Task GetEntries_EmptyDirectory_ReturnsEmpty()
    {
        // Arrange
        // Act
        var result = await mediator.Send(new GetEntriesQuery(RelativePath.Root));

        // Assert
        var entries = result.GetFolderEntriesResult();
        await Assert.That(entries.Count).IsEqualTo(0);
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
        var result = await mediator.Send(new GetEntriesQuery(RelativePath.Root));

        // Assert
        var entries = result.GetFolderEntriesResult();
        await Assert.That(entries.Count).IsEqualTo(1);
        await Assert.That(entries[0].Name).IsEqualTo("populated-folder");
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
        var entries = (await mediator.Send(new GetEntriesQuery(RelativePath.Root))).GetFolderEntriesResult();

        // Assert
        await Assert.That(entries.Count).IsEqualTo(1);
        await Assert.That(entries[0].Name).IsEqualTo("normal-folder");
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
        var entries = (await mediator.Send(new GetEntriesQuery(new RelativePath("parent")))).GetFolderEntriesResult();

        // Assert
        await Assert.That(entries.Count).IsEqualTo(1);
        await Assert.That(entries[0].Name).IsEqualTo("visible-folder");
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
        var result = await mediator.Send(new GetEntriesQuery(new RelativePath("sub")));

        // Assert
        var entries = result.GetFolderEntriesResult();
        await Assert.That(entries[0].Name).IsEqualTo("z-folder");
        await Assert.That(entries[0].Type).IsEqualTo(EntryType.Folder);
        await Assert.That(entries[1].Name).IsEqualTo("a");
        await Assert.That(entries[1].Type).IsEqualTo(EntryType.File);
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
        var result = await mediator.Send(new GetEntriesQuery(new RelativePath("sub")));

        // Assert
        var entries = result.GetFolderEntriesResult();
        await Assert.That(entries[0].Name).IsEqualTo("a-folder");
        await Assert.That(entries[1].Name).IsEqualTo("b-folder");
        await Assert.That(entries[2].Name).IsEqualTo("a-file");
        await Assert.That(entries[3].Name).IsEqualTo("z-file");
    }

    [Test]
    public async Task GetEntries_Subfolder_StripsFileExtensions()
    {
        // Arrange
        fileProvider.AddFile("sub/image.avif");
        fileProvider.AddFile("sub/readme.png");
        user.Allow("sub");

        // Act
        var entries = (await mediator.Send(new GetEntriesQuery(new RelativePath("sub")))).GetFolderEntriesResult();

        // Assert
        await Assert.That(entries.Count).IsEqualTo(2);
        await Assert.That(entries[0].Name).IsEqualTo("image");
        await Assert.That(entries[0].Path).IsEqualTo("sub/image");
        await Assert.That(entries[0].Type).IsEqualTo(EntryType.File);
        await Assert.That(entries[1].Name).IsEqualTo("readme");
        await Assert.That(entries[1].Path).IsEqualTo("sub/readme");
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
        var entries = (await mediator.Send(new GetEntriesQuery(new RelativePath("sub")))).GetFolderEntriesResult();

        // Assert
        await Assert.That(entries.Count).IsEqualTo(2);
        await Assert.That(entries[0].Name).IsEqualTo("other");
        await Assert.That(entries[0].Path).IsEqualTo("sub/other");
        await Assert.That(entries[1].Name).IsEqualTo("photo");
        await Assert.That(entries[1].Path).IsEqualTo("sub/photo");
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
        var entries = (await mediator.Send(new GetEntriesQuery(new RelativePath("sub")))).GetFolderEntriesResult();

        // Assert
        await Assert.That(entries.Count).IsEqualTo(2);
        await Assert.That(entries[0].Name).IsEqualTo("image");
        await Assert.That(entries[0].Path).IsEqualTo("sub/image");
        await Assert.That(entries[1].Name).IsEqualTo("photo");
        await Assert.That(entries[1].Path).IsEqualTo("sub/photo");
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
        var entries = (await mediator.Send(new GetEntriesQuery(new RelativePath("sub")))).GetFolderEntriesResult();

        // Assert
        await Assert.That(entries.Count).IsEqualTo(1);
        await Assert.That(entries[0].Name).IsEqualTo("photo");
        await Assert.That(entries[0].Path).IsEqualTo("sub/photo");
        await Assert.That(entries[0].Type).IsEqualTo(EntryType.File);
    }

    [Test]
    public async Task DownloadImages_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        user.IsAuthenticated = false;

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new DownloadImagesQuery(new RelativePath("vacation"), []))).Throws<NotAuthenticatedException>();
    }

    [Test]
    public async Task DownloadImages_BlockedFolder_ReturnsForbidden()
    {
        // Arrange
        fileProvider.AddFile("secret/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new DownloadImagesQuery(new RelativePath("secret"), []))).Throws<FolderAccessDeniedException>();
    }

    [Test]
    public async Task DownloadImages_NoImages_ReturnsNotFound()
    {
        // Arrange
        fileProvider.AddFile("empty/readme.txt");
        user.Allow("empty");

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new DownloadImagesQuery(new RelativePath("empty"), []))).Throws<NotFoundException>();
    }

    [Test]
    public async Task DownloadImages_ReturnsZipStream()
    {
        // Arrange
        fileProvider.AddFile("vacation/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new DownloadImagesQuery(new RelativePath("vacation"), []));

        // Assert
        await Assert.That(result.IsStatusCode(200)).IsTrue();
        await Assert.That(result.GetContentType()).IsEqualTo("application/zip");
    }

    [Test]
    public async Task DownloadImages_ValidFormat_ReturnsZipStream()
    {
        // Arrange
        fileProvider.AddFile("vacation/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("vacation/picture.jpg", imageFactory.CreateTestImage(MagickFormat.Jpeg));
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new DownloadImagesQuery(new RelativePath("vacation"), ["avif"]));

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

        var files = imageEnumerator.EnumerateImages(new RelativePath("album"), recursive: true).ToList();

        // Act
        using var memory = new MemoryStream();
        await DownloadImagesQueryHandler.WriteZipAsync(files, memory, CancellationToken.None, new RelativePath("album"));
        memory.Position = 0;
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);

        // Assert
        var entries = archive.Entries.Select(entry => entry.FullName).ToList();
        await Assert.That(entries.Count).IsEqualTo(2);
        await Assert.That(entries).Contains("photo.avif");
        await Assert.That(entries).Contains("sub/nested.jpg");
        await Assert.That(entries.Any(name => name.Contains("thumb", StringComparison.OrdinalIgnoreCase))).IsFalse();
        await Assert.That(entries.Any(name => name.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))).IsFalse();
    }

    [Test]
    public async Task DownloadImages_TrailingSlashFolder_StripsFolderPrefixFromEntries()
    {
        // Arrange
        fileProvider.AddFile("album/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("album/sub/nested.jpg", imageFactory.CreateTestImage(MagickFormat.Jpeg));
        user.Allow("album");

        // Act
        var result = await mediator.Send(new DownloadImagesQuery(new RelativePath("album/"), ["avif", "jpg"]));

        // Assert
        await Assert.That(result.IsStatusCode(200)).IsTrue();
        await Assert.That(result.GetContentType()).IsEqualTo("application/zip");
    }

    [Test]
    public async Task DownloadImages_UnsupportedFormat_ReturnsBadRequest()
    {
        // Arrange
        fileProvider.AddFile("vacation/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        user.Allow("vacation");

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new DownloadImagesQuery(new RelativePath("vacation"), ["gif"]))).Throws<BadRequestException>();
    }

    [Test]
    public async Task DownloadImages_FormatFilter_OnlyIncludesMatchingExtension()
    {
        // Arrange
        fileProvider.AddFile("album/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("album/picture.jpg", imageFactory.CreateTestImage(MagickFormat.Jpeg));
        fileProvider.AddFile("album/drawing.png", imageFactory.CreateTestImage(MagickFormat.Png));
        user.Allow("album");

        var files = imageEnumerator.EnumerateImages(new RelativePath("album"), recursive: true)
            .Where(file => string.Equals(Path.GetExtension(file.Info.Name).TrimStart('.'), "jpg", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Act
        using var memory = new MemoryStream();
        await DownloadImagesQueryHandler.WriteZipAsync(files, memory, CancellationToken.None, new RelativePath("album"));
        memory.Position = 0;
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);

        // Assert
        var entries = archive.Entries.Select(entry => entry.FullName).ToList();
        await Assert.That(entries.Count).IsEqualTo(1);
        await Assert.That(entries).Contains("picture.jpg");
    }

    [Test]
    public async Task DownloadImages_FormatFilter_NoMatches_ReturnsNotFound()
    {
        // Arrange
        fileProvider.AddFile("album/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        user.Allow("album");

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new DownloadImagesQuery(new RelativePath("album"), ["jpg"]))).Throws<NotFoundException>();
    }

    [Test]
    public async Task DownloadImages_ZipEntriesUseNoCompression()
    {
        // Arrange
        var photo = imageFactory.CreateTestImage(MagickFormat.Avif);
        fileProvider.AddFile("vacation/photo.avif", photo);
        user.Allow("vacation");
        var files = imageEnumerator.EnumerateImages(new RelativePath("vacation"), recursive: true).ToList();

        // Act
        using var memory = new MemoryStream();
        await DownloadImagesQueryHandler.WriteZipAsync(files, memory, CancellationToken.None, new RelativePath("vacation"));
        memory.Position = 0;
        using var archive = new ZipArchive(memory, ZipArchiveMode.Read);

        // Assert
        var entry = archive.Entries.Single();
        await Assert.That(entry.CompressedLength).IsEqualTo(entry.Length);
    }

    [Test]
    public async Task DownloadImages_UsageAgreementNotAccepted_ReturnsForbidden()
    {
        // Arrange
        fileProvider.AddFile("vacation/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        user.Allow("vacation");
        usageAgreement.IsEnabled = true;
        usageAgreement.IsAccepted = false;

        // Act & Assert
        await Assert.That(async () => await mediator.Send(new DownloadImagesQuery(new RelativePath("vacation"), []))).Throws<UsageAgreementNotAcceptedException>();
    }

    [Test]
    public async Task DownloadImages_UsageAgreementAccepted_ReturnsZip()
    {
        // Arrange
        fileProvider.AddFile("vacation/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        user.Allow("vacation");
        usageAgreement.IsEnabled = true;
        usageAgreement.IsAccepted = true;

        // Act
        var result = await mediator.Send(new DownloadImagesQuery(new RelativePath("vacation"), []));

        // Assert
        await Assert.That(result).IsNotNull();
    }

    [Test]
    public async Task GetRandomImage_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        user.IsAuthenticated = false;

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new GetRandomImageQuery(new RelativePath("vacation"), Accept: ""))).Throws<NotAuthenticatedException>();
    }

    [Test]
    public async Task GetRandomImage_BlockedFolder_ReturnsForbidden()
    {
        // Arrange
        fileProvider.AddFile("secret/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new GetRandomImageQuery(new RelativePath("secret"), Accept: ""))).Throws<FolderAccessDeniedException>();
    }

    [Test]
    public async Task GetRandomImage_NoImages_ReturnsNotFound()
    {
        // Arrange
        fileProvider.AddFile("empty/readme.txt");
        user.Allow("empty");

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new GetRandomImageQuery(new RelativePath("empty"), Accept: ""))).Throws<NotFoundException>();
    }

    [Test]
    public async Task GetRandomImage_ReturnsImage()
    {
        // Arrange
        fileProvider.AddFile("vacation/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new GetRandomImageQuery(new RelativePath("vacation"), Accept: ""));

        // Assert
        await Assert.That(result.IsStatusCode(200)).IsTrue();
        var fileResult = result.GetFileResult();
        await Assert.That(fileResult.ContentType).IsEqualTo("image/avif");
    }

    [Test]
    public async Task GetRandomImage_PicksRandomlyRecursively()
    {
        // Arrange
        fileProvider.AddFile("photos/album-a/a.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("photos/album-b/sub/b.jpg", imageFactory.CreateTestImage(MagickFormat.Jpeg));
        user.Allow("photos");
        var gotA = false;
        var gotB = false;

        // Act
        for (var i = 0; i < 50; i++)
        {
            var result = await mediator.Send(new GetRandomImageQuery(new RelativePath("photos"), Recursive: true));
            await Assert.That(result.IsStatusCode(200)).IsTrue();
            var fileResult = result.GetFileResult();
            var served = fileResult.FileStream.ReadAllBytes();
            if (served.SequenceEqual(fileProvider.GetFileInfo("photos/album-a/a.avif").ReadAllBytes()))
            {
                gotA = true;
            }
            else if (served.SequenceEqual(fileProvider.GetFileInfo("photos/album-b/sub/b.jpg").ReadAllBytes()))
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
        // Assert
        await Assert.That(async () => await mediator.Send(new GetRandomImageQuery(new RelativePath("photos"), Thumbnail: true))).Throws<NotAuthenticatedException>();
    }

    [Test]
    [Arguments("../etc")]
    [Arguments("/etc")]
    [Arguments("/etc/passwd")]
    public async Task GetRandomImage_Thumbnail_UnsafePath_ThrowsArgumentException(string path) =>
        await Assert.That(async () => await mediator.Send(new GetRandomImageQuery(new RelativePath(path), Thumbnail: true))).Throws<ArgumentException>();

    [Test]
    public async Task GetRandomImage_Thumbnail_BlockedFolder_ReturnsForbidden()
    {
        // Arrange
        fileProvider.AddFile("secret/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new GetRandomImageQuery(new RelativePath("secret"), Thumbnail: true))).Throws<FolderAccessDeniedException>();
    }

    [Test]
    public async Task GetRandomImage_Thumbnail_NoImageFiles_ReturnsNotFound()
    {
        // Arrange
        fileProvider.AddFile("empty/readme.txt");
        user.Allow("empty");

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new GetRandomImageQuery(new RelativePath("empty"), Thumbnail: true))).Throws<NotFoundException>();
    }

    [Test]
    public async Task GetRandomImage_Thumbnail_ReturnsThumbnail()
    {
        // Arrange
        fileProvider.AddFile("vacation/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("vacation/photo.thumb.jpg", imageFactory.CreateThumbnail());
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new GetRandomImageQuery(new RelativePath("vacation"), Thumbnail: true));

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
            var result = await mediator.Send(new GetRandomImageQuery(new RelativePath("vacation"), Thumbnail: true));
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
        // Assert
        await Assert.That(async () => await mediator.Send(new GetRandomImageQuery(new RelativePath("vacation"), Thumbnail: true))).Throws<NotFoundException>();
    }

    [Test]
    public async Task GetRandomImageFromRoots_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        user.IsAuthenticated = false;

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new GetRandomImageQuery(RelativePath.Root, Recursive: true, Accept: ""))).Throws<NotAuthenticatedException>();
    }

    [Test]
    public async Task GetRandomImageFromRoots_RecursiveFalse_ThrowsBadRequest()
    {
        // Arrange
        fileProvider.AddFile("vacation/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        user.Allow("vacation");

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new GetRandomImageQuery(RelativePath.Root, Recursive: false))).Throws<BadRequestException>();
    }

    [Test]
    public async Task GetRandomImageFromRoots_NoAccessibleFolders_ReturnsNotFound()
    {
        // Arrange
        fileProvider.AddFile("secret/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new GetRandomImageQuery(RelativePath.Root, Recursive: true, Accept: ""))).Throws<NotFoundException>();
    }

    [Test]
    public async Task GetRandomImageFromRoots_NoImages_ReturnsNotFound()
    {
        // Arrange
        fileProvider.AddFile("empty/readme.txt");
        user.Allow("empty");

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new GetRandomImageQuery(RelativePath.Root, Recursive: true, Accept: ""))).Throws<NotFoundException>();
    }

    [Test]
    public async Task GetRandomImageFromRoots_ReturnsImage()
    {
        // Arrange
        fileProvider.AddFile("vacation/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new GetRandomImageQuery(RelativePath.Root, Recursive: true, Accept: ""));

        // Assert
        await Assert.That(result.IsStatusCode(200)).IsTrue();
        await Assert.That(result.GetFileResult().ContentType).IsEqualTo("image/avif");
    }

    [Test]
    public async Task GetRandomImageFromRoots_NeverReturnsImagesFromBlockedFolders()
    {
        // Arrange
        var allowedImage = imageFactory.CreateTestImage(MagickFormat.Avif);
        var blockedImage = imageFactory.CreateTestImage(MagickFormat.Jpeg);
        fileProvider.AddFile("vacation/allowed.avif", allowedImage);
        fileProvider.AddFile("secret/blocked.jpg", blockedImage);
        user.Allow("vacation");
        var blockedBytes = fileProvider.GetFileInfo("secret/blocked.jpg").ReadAllBytes();

        // Act
        // Assert
        for (var i = 0; i < 50; i++)
        {
            var result = await mediator.Send(new GetRandomImageQuery(RelativePath.Root, Recursive: true, Accept: "*/*"));
            await Assert.That(result.IsStatusCode(200)).IsTrue();
            var served = result.GetFileResult().FileStream.ReadAllBytes();
            await Assert.That(served.SequenceEqual(blockedBytes)).IsFalse();
        }
    }

    [Test]
    public async Task GetRandomImageFromRoots_PicksAcrossMultipleAccessibleFolders()
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
            var result = await mediator.Send(new GetRandomImageQuery(RelativePath.Root, Recursive: true, Accept: "*/*"));
            await Assert.That(result.IsStatusCode(200)).IsTrue();
            var served = result.GetFileResult().FileStream.ReadAllBytes();
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
    public async Task GetRandomImageFromRoots_Thumbnail_ReturnsThumbnail()
    {
        // Arrange
        fileProvider.AddFile("vacation/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("vacation/photo.thumb.jpg", imageFactory.CreateThumbnail());
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new GetRandomImageQuery(RelativePath.Root, Thumbnail: true, Recursive: true, Accept: "*/*"));

        // Assert
        await Assert.That(result.IsStatusCode(200)).IsTrue();
        await Assert.That(result.GetFileResult().ContentType).IsEqualTo("image/jpeg");
    }

    [Test]
    public async Task GetRandomImageFromRoots_Thumbnail_OnlyReturnsFromAccessibleFolders()
    {
        // Arrange
        var allowedThumbnail = imageFactory.CreateThumbnail(MagickColors.DodgerBlue);
        var blockedThumbnail = imageFactory.CreateThumbnail(MagickColors.Crimson);
        fileProvider.AddFile("vacation/photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("vacation/photo.thumb.jpg", allowedThumbnail);
        fileProvider.AddFile("secret/hidden.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("secret/hidden.thumb.jpg", blockedThumbnail);
        user.Allow("vacation");
        var blockedBytes = fileProvider.GetFileInfo("secret/hidden.thumb.jpg").ReadAllBytes();

        // Act
        // Assert
        for (var i = 0; i < 50; i++)
        {
            var result = await mediator.Send(new GetRandomImageQuery(RelativePath.Root, Thumbnail: true, Recursive: true, Accept: "*/*"));
            await Assert.That(result.IsStatusCode(200)).IsTrue();
            var served = result.GetFileResult().FileStream.ReadAllBytes();
            await Assert.That(served.SequenceEqual(blockedBytes)).IsFalse();
        }
    }

    [Test]
    public async Task GetRandomImage_NeverLeaksImagesFromBlockedFolders()
    {
        // Arrange — several blocked root folders each holding multiple images, plus one
        // allowed folder. Across many random draws over root-spanning and single-folder
        // recursive requests, no served image may originate from a blocked folder.
        var blockedRoots = new[] { "secret", "private", "hidden" };
        var blockedBytes = new List<byte[]>();
        foreach (var root in blockedRoots)
        {
            for (var i = 0; i < 3; i++)
            {
                var bytes = imageFactory.CreateTestImage(MagickFormat.Jpeg);
                fileProvider.AddFile($"{root}/img-{i}.jpg", bytes);
                blockedBytes.Add(bytes);
            }
        }

        var allowedBytes = imageFactory.CreateTestImage(MagickFormat.Avif);
        fileProvider.AddFile("vacation/allowed.avif", allowedBytes);
        user.Allow("vacation");

        // Act
        // Assert — root-spanning recursive draws must only ever serve the allowed image.
        for (var i = 0; i < 100; i++)
        {
            var result = await mediator.Send(new GetRandomImageQuery(RelativePath.Root, Recursive: true, Accept: "*/*"));
            await Assert.That(result.IsStatusCode(200)).IsTrue();
            var served = result.GetFileResult().FileStream.ReadAllBytes();
            await Assert.That(served.SequenceEqual(allowedBytes)).IsTrue();
            foreach (var blocked in blockedBytes)
            {
                await Assert.That(served.SequenceEqual(blocked)).IsFalse();
            }
        }
    }

    [Test]
    [Arguments("", "image/jpeg", true)]
    [Arguments("image/avif", "image/avif", true)]
    [Arguments("image/*", "image/avif", true)]
    [Arguments("*/*", "image/avif", true)]
    [Arguments("image/png,image/webp", "image/avif", false)]
    [Arguments("image/png,image/avif", "image/avif", true)]
    [Arguments("image/webp;q=0.8,image/avif;q=1.0", "image/avif", true)]
    public async Task IsFormatAccepted_MatchesExpectedBehavior(StringValues header, string format, bool expected)
    {
        // Act
        var result = header.Accepts(format);

        // Assert
        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    public async Task ServeImage_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        fileProvider.AddFile("photo.png", imageFactory.CreateTestImage(MagickFormat.Png));
        user.IsAuthenticated = false;

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new ServeImageQuery(new RelativePath("photo.png"), "", false))).Throws<NotAuthenticatedException>();
    }

    [Test]
    public async Task ServeImage_BlockedFolder_ReturnsForbidden()
    {
        // Arrange
        fileProvider.AddFile("secret/photo.png", imageFactory.CreateTestImage(MagickFormat.Png));

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new ServeImageQuery(new RelativePath("secret/photo.png"), "", false))).Throws<FolderAccessDeniedException>();
    }

    [Test]
    public async Task ServeImage_BlockedSubfolder_ReturnsForbidden()
    {
        // Arrange
        fileProvider.AddFile("secret/nested/photo.png", imageFactory.CreateTestImage(MagickFormat.Png));

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new ServeImageQuery(new RelativePath("secret/nested/photo.png"), "", false))).Throws<FolderAccessDeniedException>();
    }

    [Test]
    public async Task ServeImage_NonExistentFile_ReturnsNotFound()
    {
        // Arrange
        user.Allow("vacation");

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new ServeImageQuery(new RelativePath("missing.avif"), "", false))).Throws<NotFoundException>();
    }

    [Test]
    [Arguments("../etc")]
    [Arguments("/etc")]
    [Arguments("/etc/passwd")]
    public async Task ServeImage_UnsafePath_ThrowsArgumentException(string path) =>
        await Assert.That(async () => await mediator.Send(new ServeImageQuery(new RelativePath(path), "", false))).Throws<ArgumentException>();

    [Test]
    public async Task ServeImage_ThumbprintFile_ReturnsBadRequest()
    {
        // Arrange
        fileProvider.AddFile("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("photo.thumb.jpg", imageFactory.CreateTestImage(MagickFormat.Jpeg));
        user.Allow("vacation");

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new ServeImageQuery(new RelativePath("photo.thumb.jpg"), "", false))).Throws<BadRequestException>();
    }

    [Test]
    public async Task ServeImage_NoAcceptHeader_ServesOriginal()
    {
        // Arrange
        fileProvider.AddFile("photo.png", imageFactory.CreateTestImage(MagickFormat.Png));
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new ServeImageQuery(new RelativePath("photo.png"), "", false));

        // Assert
        await Assert.That(result.IsStatusCode(200)).IsTrue();
        await Assert.That(result.GetContentType()).IsEqualTo("image/png");
    }

    [Test]
    public async Task ServeImage_FormatAccepted_ServesOriginal()
    {
        // Arrange
        fileProvider.AddFile("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new ServeImageQuery(new RelativePath("photo.avif"), "image/avif,image/jpeg", false));

        // Assert
        await Assert.That(result.IsStatusCode(200)).IsTrue();
        await Assert.That(result.GetContentType()).IsEqualTo("image/avif");
    }

    [Test]
    public async Task ServeImage_ThumbTrue_ServesThumbprint()
    {
        // Arrange
        fileProvider.AddFile("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("photo.thumb.jpg", imageFactory.CreateTestImage(MagickFormat.Jpeg));
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new ServeImageQuery(new RelativePath("photo"), "", true));

        // Assert
        await Assert.That(result.IsStatusCode(200)).IsTrue();
        await Assert.That(result.GetContentType()).IsEqualTo("image/jpeg");
    }

    [Test]
    public async Task ServeImage_ThumbTrue_NoThumbprint_ReturnsNotFound()
    {
        // Arrange
        fileProvider.AddFile("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        user.Allow("vacation");

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new ServeImageQuery(new RelativePath("photo"), "", true))).Throws<NotFoundException>();
    }

    [Test]
    public async Task ServeImage_ThumbTrue_NoImage_ReturnsNotFound()
    {
        // Arrange
        user.Allow("vacation");

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new ServeImageQuery(new RelativePath("missing"), "", true))).Throws<NotFoundException>();
    }

    [Test]
    public async Task ServeImage_ThumbTrue_ThumbprintNotAccepted_Returns406()
    {
        // Arrange
        fileProvider.AddFile("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("photo.thumb.jpg", imageFactory.CreateTestImage(MagickFormat.Jpeg));
        user.Allow("vacation");

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new ServeImageQuery(new RelativePath("photo"), "image/webp,image/png", true))).Throws<NotAcceptableException>();
    }

    [Test]
    public async Task ServeImage_NoAcceptedFormats_Returns406()
    {
        // Arrange
        fileProvider.AddFile("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        user.Allow("vacation");

        // Act
        // Assert
        await Assert.That(async () => await mediator.Send(new ServeImageQuery(new RelativePath("photo"), "image/tiff", false))).Throws<NotAcceptableException>();
    }

    [Test]
    public async Task ServeImage_ServesSmallestFileFirst()
    {
        // Arrange
        fileProvider.AddFile("photo.png", imageFactory.CreateTestImage(10, 10, MagickFormat.Png));
        fileProvider.AddFile("photo.jpg", imageFactory.CreateTestImage(100, 100, MagickFormat.Jpeg));
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new ServeImageQuery(new RelativePath("photo"), "image/jpeg,image/png", false));

        // Assert
        await Assert.That(result.IsStatusCode(200)).IsTrue();
        await Assert.That(result.GetContentType()).IsEqualTo("image/png");
    }

    [Test]
    public async Task ServeImage_ThumbTrue_ServesSmallestThumbprintFirst()
    {
        // Arrange
        fileProvider.Write("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("photo.thumb.jpg", imageFactory.CreateTestImage(10, 10, MagickFormat.Jpeg));
        fileProvider.AddFile("photo.thumb.png", imageFactory.CreateTestImage(100, 100, MagickFormat.Png));
        user.Allow("vacation");

        // Act
        var result = await mediator.Send(new ServeImageQuery(new RelativePath("photo"), "image/jpeg,image/png", true));

        // Assert
        await Assert.That(result.IsStatusCode(200)).IsTrue();
        await Assert.That(result.GetContentType()).IsEqualTo("image/jpeg");
    }

    [Test]
    public async Task ServeImage_FullImage_UsageAgreementNotAccepted_ReturnsForbidden()
    {
        // Arrange
        fileProvider.Write("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        user.Allow("vacation");
        usageAgreement.IsEnabled = true;
        usageAgreement.IsAccepted = false;

        // Act & Assert
        await Assert.That(async () => await mediator.Send(new ServeImageQuery(new RelativePath("photo"), "", false))).Throws<UsageAgreementNotAcceptedException>();
    }

    [Test]
    public async Task ServeImage_Thumbnail_UsageAgreementNotAccepted_IsAllowed()
    {
        // Arrange
        fileProvider.Write("photo.avif", imageFactory.CreateTestImage(MagickFormat.Avif));
        fileProvider.AddFile("photo.thumb.jpg", imageFactory.CreateTestImage(10, 10, MagickFormat.Jpeg));
        user.Allow("vacation");
        usageAgreement.IsEnabled = true;
        usageAgreement.IsAccepted = false;

        // Act
        var result = await mediator.Send(new ServeImageQuery(new RelativePath("photo"), "image/jpeg", true));

        // Assert
        await Assert.That(result.IsStatusCode(200)).IsTrue();
    }
}
