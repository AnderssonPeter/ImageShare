using ImageShare.Browsing;

namespace ImageShare.Tests;

[MicrosoftDI]
public class PathHelperTests
{
    [Test]
    [Arguments("normal/path")]
    [Arguments("file.txt")]
    [Arguments("folder/subfolder/file.png")]
    [Arguments("")]
    [Arguments("photo")]
    public async Task EnsureSafePath_SafePath_DoesNotThrow(string path) =>
        await Assert.That(() => PathHelper.EnsureSafePath(path)).ThrowsNothing();

    [Test]
    [Arguments("../etc")]
    [Arguments("..\\etc")]
    [Arguments("foo/../bar")]
    [Arguments("FOO/..")]
    [Arguments("/etc/passwd")]
    [Arguments("/")]
    [Arguments("/etc")]
    public async Task EnsureSafePath_UnsafePath_ThrowsArgumentException(string path) =>
        await Assert.That(() => PathHelper.EnsureSafePath(path)).Throws<ArgumentException>();

    [Test]
    [Arguments("foo/bar", "foo")]
    [Arguments("foo", "foo")]
    [Arguments("foo/bar/baz", "foo")]
    [Arguments("", "")]
    public async Task GetFirstSegment_ReturnsFirstSegment(string path, string expected) =>
        await Assert.That(PathHelper.GetFirstSegment(path)).IsEqualTo(expected);

    [Test]
    [Arguments("foo/bar", true)]
    [Arguments("foo/bar/baz", true)]
    [Arguments("foo", false)]
    [Arguments("", false)]
    public async Task IsInFolder_ReturnsExpectedResult(string path, bool expected) =>
        await Assert.That(PathHelper.IsInFolder(path)).IsEqualTo(expected);

    [Test]
    public async Task Combine_SafePaths_ReturnsCombinedPath() =>
        await Assert.That(PathHelper.Combine("foo", "bar")).IsEqualTo(Path.Combine("foo", "bar"));

    [Test]
    [Arguments("../etc", "bar")]
    [Arguments("foo", "../etc")]
    [Arguments("/etc", "bar")]
    [Arguments("foo", "/etc")]
    public async Task Combine_UnsafePath_ThrowsArgumentException(string path1, string path2) =>
        await Assert.That(() => PathHelper.Combine(path1, path2)).Throws<ArgumentException>();
}
