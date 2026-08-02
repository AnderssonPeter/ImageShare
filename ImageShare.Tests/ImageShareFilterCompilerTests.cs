using ImageShare.Authentication;

namespace ImageShare.Tests;

[MicrosoftDI]
public class ImageShareFilterCompilerTests(ImageShareFilterCompiler compiler)
{
    [Test]
    [Arguments("hello", "hello", true)]
    [Arguments("hello", "Hello", true)]
    [Arguments("hello", "hello/", false)]
    [Arguments("hello", "xhello", false)]
    [Arguments("hello", "foobar", false)]
    [Arguments("images/*.jpg", "images/photo.jpg", true)]
    [Arguments("images/*.jpg", "images/sub/photo.jpg", false)]
    [Arguments("images/*.jpg", "images/.jpg", true)]
    [Arguments("images/*.jpg", "images/", false)]
    [Arguments("file?.txt", "file1.txt", true)]
    [Arguments("file?.txt", "fileA.txt", true)]
    [Arguments("file?.txt", "file12.txt", false)]
    [Arguments("file?.txt", "file.txt", false)]
    [Arguments("*.jpg|*.png|*.gif", "photo.jpg", true)]
    [Arguments("*.jpg|*.png|*.gif", "icon.png", true)]
    [Arguments("*.jpg|*.png|*.gif", "anim.gif", true)]
    [Arguments("*.jpg|*.png|*.gif", "doc.pdf", false)]
    [Arguments("*.jpg|*.png|*.gif", "photo.JPG", true)]
    [Arguments("FILE.TXT", "file.txt", true)]
    [Arguments("FILE.TXT", "FILE.TXT", true)]
    [Arguments("FILE.TXT", "File.Txt", true)]
    [Arguments("foo", "foobar", false)]
    [Arguments("foo", "xfoo", false)]
    [Arguments("cost[0-9].txt", "cost[0-9].txt", true)]
    [Arguments("cost[0-9].txt", "cost0.txt", false)]
    [Arguments("src/**/build/*.dll|src/*.exe", "src/project/build/output.dll", true)]
    [Arguments("src/**/build/*.dll|src/*.exe", "src/app.exe", true)]
    [Arguments("src/**/build/*.dll|src/*.exe", "src/project/build/sub/output.dll", false)]
    [Arguments("*.log", "errors.log", true)]
    [Arguments("*.log", "app.log", true)]
    [Arguments("*.log", ".log", true)]
    [Arguments("file.txt", "file.txt", true)]
    [Arguments("file.txt", "file_txt", false)]
    public async Task Compile_Pattern_MatchesExpected(string filter, string input, bool expected)
    {
        // Act
        var regex = compiler.Compile(filter);

        // Assert
        await Assert.That(regex.IsMatch(input)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    public async Task Compile_NullOrEmptyInput_ThrowsArgumentException(string? input) =>
        await Assert.That(() => compiler.Compile(input!)).Throws<ArgumentException>();

    [Test]
    [Arguments(" ")]
    [Arguments("\t")]
    [Arguments("  ")]
    public async Task Compile_WhitespaceInput_ThrowsArgumentException(string input) =>
        await Assert.That(() => compiler.Compile(input)).Throws<ArgumentException>();

    [Test]
    [Arguments("!secret")]
    [Arguments("!a|!b")]
    [Arguments("|!only")]
    public async Task Compile_DenyOnlyFilter_ThrowsArgumentException(string filter) =>
        await Assert.That(() => compiler.Compile(filter)).Throws<ArgumentException>();

    [Test]
    [Arguments("*|!secret|")]
    [Arguments("!")]
    public async Task Compile_EmptyPattern_ThrowsArgumentException(string filter) =>
        await Assert.That(() => compiler.Compile(filter)).Throws<ArgumentException>();

    [Test]
    [Arguments("*|!test", "test", false)]
    [Arguments("*|!test", "other", true)]
    [Arguments("*|!test", "TEST", false)]
    [Arguments("*|!test", "subfolder", true)]
    [Arguments("*.jpg|!bad.jpg", "photo.jpg", true)]
    [Arguments("*.jpg|!bad.jpg", "bad.jpg", false)]
    [Arguments("*.jpg|!bad.jpg", "bad.JPG", false)]
    [Arguments("*|!a|!b", "a", false)]
    [Arguments("*|!a|!b", "b", false)]
    [Arguments("*|!a|!b", "c", true)]
    [Arguments("album-*|!album-secret", "album-vacation", true)]
    [Arguments("album-*|!album-secret", "album-secret", false)]
    [Arguments("test|!test", "test", false)]
    [Arguments("a|b|!a", "a", false)]
    [Arguments("a|b|!a", "b", true)]
    public async Task Compile_WithDeny_DenyAlwaysWins(string filter, string input, bool expected)
    {
        // Act
        var regex = compiler.Compile(filter);

        // Assert
        await Assert.That(regex.IsMatch(input)).IsEqualTo(expected);
    }

    [Test]
    public async Task Compile_SameInput_ReturnsCachedInstance()
    {
        // Act
        var regex1 = compiler.Compile("cached-pattern");
        var regex2 = compiler.Compile("cached-pattern");

        // Assert
        await Assert.That(regex1).IsSameReferenceAs(regex2);
    }

    [Test]
    public async Task Compile_DifferentInputs_ReturnDifferentInstances()
    {
        // Act
        var regex1 = compiler.Compile("pattern-a");
        var regex2 = compiler.Compile("pattern-b");

        // Assert
        await Assert.That(regex1).IsNotSameReferenceAs(regex2);
    }

    [Test]
    public async Task Compile_DenyPattern_CachedSeparatelyFromAllowOnly()
    {
        // Act
        var allowOnly = compiler.Compile("*");
        var withDeny = compiler.Compile("*|!test");

        // Assert
        await Assert.That(allowOnly).IsNotSameReferenceAs(withDeny);
        await Assert.That(allowOnly.IsMatch("test")).IsTrue();
        await Assert.That(withDeny.IsMatch("test")).IsFalse();
    }
}
