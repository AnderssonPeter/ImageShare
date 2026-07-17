using ImageShare.Authentication;

namespace ImageShare.Tests;

[MicrosoftDI]
public class ImageShareFilterServiceTests(ImageShareFilterService service)
{
    [Test]
    public async Task GetImageShareFilterRegex_LiteralPattern_MatchesExactString()
    {
        var regex = service.GetImageShareFilterRegex("hello");

        await Assert.That(regex.IsMatch("hello")).IsTrue();
        await Assert.That(regex.IsMatch("Hello")).IsTrue();
        await Assert.That(regex.IsMatch("hello/")).IsFalse();
        await Assert.That(regex.IsMatch("xhello")).IsFalse();
    }

    [Test]
    public async Task GetImageShareFilterRegex_WildcardStar_MatchesAnyNonSlashSequence()
    {
        var regex = service.GetImageShareFilterRegex("images/*.jpg");

        await Assert.That(regex.IsMatch("images/photo.jpg")).IsTrue();
        await Assert.That(regex.IsMatch("images/sub/photo.jpg")).IsFalse();
        await Assert.That(regex.IsMatch("images/.jpg")).IsTrue();
        await Assert.That(regex.IsMatch("images/")).IsFalse();
    }

    [Test]
    public async Task GetImageShareFilterRegex_WildcardQuestion_MatchesSingleNonSlashChar()
    {
        var regex = service.GetImageShareFilterRegex("file?.txt");

        await Assert.That(regex.IsMatch("file1.txt")).IsTrue();
        await Assert.That(regex.IsMatch("fileA.txt")).IsTrue();
        await Assert.That(regex.IsMatch("file12.txt")).IsFalse();
        await Assert.That(regex.IsMatch("file.txt")).IsFalse();
    }

    [Test]
    public async Task GetImageShareFilterRegex_MultiplePatterns_MatchesAnyOfThem()
    {
        var regex = service.GetImageShareFilterRegex("*.jpg|*.png|*.gif");

        await Assert.That(regex.IsMatch("photo.jpg")).IsTrue();
        await Assert.That(regex.IsMatch("icon.png")).IsTrue();
        await Assert.That(regex.IsMatch("anim.gif")).IsTrue();
        await Assert.That(regex.IsMatch("doc.pdf")).IsFalse();
        await Assert.That(regex.IsMatch("photo.JPG")).IsTrue();
    }

    [Test]
    public async Task GetImageShareFilterRegex_CaseInsensitive_MatchesRegardlessOfCase()
    {
        var regex = service.GetImageShareFilterRegex("FILE.TXT");

        await Assert.That(regex.IsMatch("file.txt")).IsTrue();
        await Assert.That(regex.IsMatch("FILE.TXT")).IsTrue();
        await Assert.That(regex.IsMatch("File.Txt")).IsTrue();
    }

    [Test]
    public async Task GetImageShareFilterRegex_Anchored_DoesNotMatchPartialString()
    {
        var regex = service.GetImageShareFilterRegex("foo");

        await Assert.That(regex.IsMatch("foo")).IsTrue();
        await Assert.That(regex.IsMatch("foobar")).IsFalse();
        await Assert.That(regex.IsMatch("xfoo")).IsFalse();
    }

    [Test]
    public async Task GetImageShareFilterRegex_SpecialRegexChars_AreEscaped()
    {
        var regex = service.GetImageShareFilterRegex("cost[0-9].txt");

        await Assert.That(regex.IsMatch("cost[0-9].txt")).IsTrue();
        await Assert.That(regex.IsMatch("cost0.txt")).IsFalse();
    }

    [Test]
    public async Task GetImageShareFilterRegex_ComplexPattern_MatchesCorrectly()
    {
        var regex = service.GetImageShareFilterRegex("src/**/build/*.dll|src/*.exe");

        await Assert.That(regex.IsMatch("src/project/build/output.dll")).IsTrue();
        await Assert.That(regex.IsMatch("src/app.exe")).IsTrue();
        await Assert.That(regex.IsMatch("src/project/build/sub/output.dll")).IsFalse();
    }

    [Test]
    public async Task GetImageShareFilterRegex_NullInput_ThrowsArgumentException() =>
        await Assert.That(() => service.GetImageShareFilterRegex(null!)).Throws<ArgumentException>();

    [Test]
    public async Task GetImageShareFilterRegex_EmptyInput_ThrowsArgumentException() =>
        await Assert.That(() => service.GetImageShareFilterRegex("")).Throws<ArgumentException>();

    [Test]
    [Arguments(" ")]
    [Arguments("\t")]
    [Arguments("  ")]
    public async Task GetImageShareFilterRegex_WhitespaceInput_ThrowsArgumentException(string input) =>
        await Assert.That(() => service.GetImageShareFilterRegex(input)).Throws<ArgumentException>();

    [Test]
    public async Task GetImageShareFilterRegex_SameInput_ReturnsCachedInstance()
    {
        var regex1 = service.GetImageShareFilterRegex("cached-pattern");
        var regex2 = service.GetImageShareFilterRegex("cached-pattern");

        await Assert.That(regex1).IsSameReferenceAs(regex2);
    }

    [Test]
    public async Task GetImageShareFilterRegex_DifferentInputs_ReturnDifferentInstances()
    {
        var regex1 = service.GetImageShareFilterRegex("pattern-a");
        var regex2 = service.GetImageShareFilterRegex("pattern-b");

        await Assert.That(regex1).IsNotSameReferenceAs(regex2);
    }

    [Test]
    public async Task GetImageShareFilterRegex_StarAtStart_MatchesEverything()
    {
        var regex = service.GetImageShareFilterRegex("*.log");

        await Assert.That(regex.IsMatch("errors.log")).IsTrue();
        await Assert.That(regex.IsMatch("app.log")).IsTrue();
        await Assert.That(regex.IsMatch(".log")).IsTrue();
    }

    [Test]
    public async Task GetImageShareFilterRegex_EscapedPattern_HandlesDotCorrectly()
    {
        var regex = service.GetImageShareFilterRegex("file.txt");

        await Assert.That(regex.IsMatch("file.txt")).IsTrue();
        await Assert.That(regex.IsMatch("file_txt")).IsFalse();
    }
}