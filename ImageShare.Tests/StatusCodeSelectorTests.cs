using ImageShare.Browsing;
using ImageShare.Errors;
using Microsoft.AspNetCore.Http;

namespace ImageShare.Tests;

public class StatusCodeSelectorTests
{
    public static IEnumerable<(ImageShareException exception, int expectedStatusCode)> ExceptionCases =>
    [
        (new NotAuthenticatedException(), StatusCodes.Status401Unauthorized),
        (new BadRequestException("bad input"), StatusCodes.Status400BadRequest),
        (new FolderAccessDeniedException(RelativePath.Root), StatusCodes.Status403Forbidden),
        (new NotFoundException("missing"), StatusCodes.Status404NotFound),
        (new NotAcceptableException("not accepted"), StatusCodes.Status406NotAcceptable),
    ];

    [Test]
    [MethodDataSource(nameof(ExceptionCases))]
    public async Task SelectStatusCode_MapsExceptionToCorrectStatusCode(ImageShareException exception, int expectedStatusCode)
    {
        // Act
        var actualStatusCode = ErrorExtensions.SelectStatusCode(exception);

        // Assert
        await Assert.That(actualStatusCode).IsEqualTo(expectedStatusCode);
    }

    [Test]
    public async Task SelectStatusCode_UnknownException_ReturnsInternalServerError()
    {
        // Arrange
        var exception = new InvalidOperationException("unexpected");

        // Act
        var actualStatusCode = ErrorExtensions.SelectStatusCode(exception);

        // Assert
        await Assert.That(actualStatusCode).IsEqualTo(StatusCodes.Status500InternalServerError);
    }
}
