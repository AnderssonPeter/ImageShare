using ImageShare.Authentication;

namespace ImageShare.Tests;

[MicrosoftDI]
public class ImageShareFilterCompilerTests(ImageShareFilterCompiler compiler)
{
    [Test]
    public async Task Compile_LiteralPattern_MatchesExactString()
    {
        // Act
        var regex = compiler.Compile("hello");

        // Assert
        await Assert.That(regex.IsMatch("hello")).IsTrue();
        await Assert.That(regex.IsMatch("Hello")).IsTrue();
        await Assert.That(regex.IsMatch("hello/")).IsFalse();
        await Assert.That(regex.IsMatch("xhello")).IsFalse();
    }

    [Test]
    public async Task Compile_WildcardStar_MatchesAnyNonSlashSequence()
    {
        // Act
        var regex = compiler.Compile("images/*.jpg");

        // Assert
        await Assert.That(regex.IsMatch("images/photo.jpg")).IsTrue();
        await Assert.That(regex.IsMatch("images/sub/photo.jpg")).IsFalse();
        await Assert.That(regex.IsMatch("images/.jpg")).IsTrue();
        await Assert.That(regex.IsMatch("images/")).IsFalse();
    }

    [Test]
    public async Task Compile_WildcardQuestion_MatchesSingleNonSlashChar()
    {
        // Act
        var regex = compiler.Compile("file?.txt");

        // Assert
        await Assert.That(regex.IsMatch("file1.txt")).IsTrue();
        await Assert.That(regex.IsMatch("fileA.txt")).IsTrue();
        await Assert.That(regex.IsMatch("file12.txt")).IsFalse();
        await Assert.That(regex.IsMatch("file.txt")).IsFalse();
    }

    [Test]
    public async Task Compile_MultiplePatterns_MatchesAnyOfThem()
    {
        // Act
        var regex = compiler.Compile("*.jpg|*.png|*.gif");

        // Assert
        await Assert.That(regex.IsMatch("photo.jpg")).IsTrue();
        await Assert.That(regex.IsMatch("icon.png")).IsTrue();
        await Assert.That(regex.IsMatch("anim.gif")).IsTrue();
        await Assert.That(regex.IsMatch("doc.pdf")).IsFalse();
        await Assert.That(regex.IsMatch("photo.JPG")).IsTrue();
    }

    [Test]
    public async Task Compile_CaseInsensitive_MatchesRegardlessOfCase()
    {
        // Act
        var regex = compiler.Compile("FILE.TXT");

        // Assert
        await Assert.That(regex.IsMatch("file.txt")).IsTrue();
        await Assert.That(regex.IsMatch("FILE.TXT")).IsTrue();
        await Assert.That(regex.IsMatch("File.Txt")).IsTrue();
    }

    [Test]
    public async Task Compile_Anchored_DoesNotMatchPartialString()
    {
        // Act
        var regex = compiler.Compile("foo");

        // Assert
        await Assert.That(regex.IsMatch("foo")).IsTrue();
        await Assert.That(regex.IsMatch("foobar")).IsFalse();
        await Assert.That(regex.IsMatch("xfoo")).IsFalse();
    }

    [Test]
    public async Task Compile_SpecialRegexChars_AreEscaped()
    {
        // Act
        var regex = compiler.Compile("cost[0-9].txt");

        // Assert
        await Assert.That(regex.IsMatch("cost[0-9].txt")).IsTrue();
        await Assert.That(regex.IsMatch("cost0.txt")).IsFalse();
    }

    [Test]
    public async Task Compile_ComplexPattern_MatchesCorrectly()
    {
        // Act
        var regex = compiler.Compile("src/**/build/*.dll|src/*.exe");

        // Assert
        await Assert.That(regex.IsMatch("src/project/build/output.dll")).IsTrue();
        await Assert.That(regex.IsMatch("src/app.exe")).IsTrue();
        await Assert.That(regex.IsMatch("src/project/build/sub/output.dll")).IsFalse();
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
    public async Task Compile_StarAtStart_MatchesEverything()
    {
        // Act
        var regex = compiler.Compile("*.log");

        // Assert
        await Assert.That(regex.IsMatch("errors.log")).IsTrue();
        await Assert.That(regex.IsMatch("app.log")).IsTrue();
        await Assert.That(regex.IsMatch(".log")).IsTrue();
    }

    [Test]
    public async Task Compile_EscapedPattern_HandlesDotCorrectly()
    {
        // Act
        var regex = compiler.Compile("file.txt");

        // Assert
        await Assert.That(regex.IsMatch("file.txt")).IsTrue();
        await Assert.That(regex.IsMatch("file_txt")).IsFalse();
    }
}
