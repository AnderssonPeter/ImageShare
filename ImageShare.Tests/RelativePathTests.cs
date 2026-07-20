using ImageShare.Browsing;

namespace ImageShare.Tests;

[MicrosoftDI]
public class RelativePathTests
{
    [Test]
    [Arguments("normal/path")]
    [Arguments("file.txt")]
    [Arguments("folder/subfolder/file.png")]
    [Arguments("")]
    [Arguments("photo")]
    public async Task Constructor_SafePath_DoesNotThrow(string path) =>
        await Assert.That(() => new RelativePath(path)).ThrowsNothing();

    [Test]
    [Arguments("../etc")]
    [Arguments("..\\etc")]
    [Arguments("foo/../bar")]
    [Arguments("FOO/..")]
    [Arguments("/etc/passwd")]
    [Arguments("/")]
    [Arguments("/etc")]
    public async Task Constructor_UnsafePath_ThrowsArgumentException(string path) =>
        await Assert.That(() => new RelativePath(path)).Throws<ArgumentException>();

    [Test]
    [Arguments("foo/bar", "foo")]
    [Arguments("foo", "foo")]
    [Arguments("foo/bar/baz", "foo")]
    [Arguments("", "")]
    public async Task FirstSegment_ReturnsFirstSegment(string path, string expected) =>
        await Assert.That(new RelativePath(path).RootFolder).IsEqualTo(expected);

    [Test]
    [Arguments("foo/bar", true)]
    [Arguments("foo/bar/baz", true)]
    [Arguments("foo", false)]
    [Arguments("", false)]
    public async Task IsInFolder_ReturnsExpectedResult(string path, bool expected) =>
        await Assert.That(new RelativePath(path).IsInFolder).IsEqualTo(expected);

    [Test]
    public async Task Combine_SafePaths_ReturnsCombinedPath() =>
        await Assert.That(new RelativePath("foo").Combine("bar").Value).IsEqualTo("foo/bar");

    [Test]
    [Arguments("../etc", "bar")]
    [Arguments("foo", "../bar")]
    [Arguments("/etc", "bar")]
    [Arguments("foo", "/etc")]
    public async Task Combine_UnsafePath_ThrowsArgumentException(string basePath, string child) =>
        await Assert.That(() => new RelativePath(basePath).Combine(child)).Throws<ArgumentException>();

    [Test]
    [Arguments("photo.jpg", "photo")]
    [Arguments("folder/photo.png", "photo")]
    [Arguments("photo", "photo")]
    public async Task FileNameWithoutExtension_ReturnsNameWithoutExtension(string path, string expected) =>
        await Assert.That(new RelativePath(path).FileNameWithoutExtension).IsEqualTo(expected);

    [Test]
    [Arguments("photo.jpg", "jpg")]
    [Arguments("photo.avif", "avif")]
    [Arguments("photo.thumb.jpg", "jpg")]
    [Arguments("photo", null)]
    public async Task Extension_ReturnsExtensionWithoutDot(string path, string? expected) =>
        await Assert.That(new RelativePath(path).Extension).IsEqualTo(expected);

    [Test]
    [Arguments("photo.jpg", true)]
    [Arguments("photo.avif", true)]
    [Arguments("photo", false)]
    public async Task HasExtension_ReturnsExpectedResult(string path, bool expected) =>
        await Assert.That(new RelativePath(path).HasExtension).IsEqualTo(expected);

    [Test]
    [Arguments("photo.thumb.jpg", true)]
    [Arguments("photo.thumb.png", true)]
    [Arguments("photo.avif", false)]
    [Arguments("photo.jpg", false)]
    public async Task IsThumbnail_DetectsThumbnailInfix(string path, bool expected) =>
        await Assert.That(new RelativePath(path).IsThumbnail).IsEqualTo(expected);

    [Test]
    [Arguments("folder/photo.jpg", "folder")]
    [Arguments("photo.jpg", "")]
    public async Task Directory_ReturnsDirectory(string path, string expected) =>
        await Assert.That(new RelativePath(path).Directory).IsEqualTo(expected);

    [Test]
    [Arguments("photo.jpg", "photo.jpg")]
    [Arguments("folder/photo.png", "photo.png")]
    public async Task FileName_ReturnsFileName(string path, string expected) =>
        await Assert.That(new RelativePath(path).FileName).IsEqualTo(expected);
}
