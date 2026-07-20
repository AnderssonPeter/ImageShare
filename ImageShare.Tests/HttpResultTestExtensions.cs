using ImageShare.Browsing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ImageShare.Tests;

internal static class HttpResultTestExtensions
{
    public static IResult Unwrap(this IResult result) =>
        result is INestedHttpResult nested ? (IResult)nested.Result : result;

    public static bool IsStatusCode(this IResult result, int statusCode)
    {
        return result.Unwrap() switch
        {
            UnauthorizedHttpResult => statusCode == 401,
            NotFound => statusCode == 404,
            BadRequest => statusCode == 400,
            ForbidHttpResult => statusCode == 403,
            StatusCodeHttpResult statusResult => statusResult.StatusCode == statusCode,
            FileStreamHttpResult => statusCode == 200,
            Ok<PaginatedResult<FolderEntry>> => statusCode == 200,
            IStatusCodeHttpResult statusCodeResult => statusCodeResult.StatusCode == statusCode,
            _ => statusCode == 200,
        };
    }

    public static PaginatedResult<FolderEntry> GetFolderEntriesResult(this IResult result) =>
        ((Ok<PaginatedResult<FolderEntry>>)result.Unwrap()).Value!;

    public static FileStreamHttpResult GetFileResult(this IResult result) =>
        (FileStreamHttpResult)result.Unwrap();

    public static string? GetContentType(this IResult result)
    {
        var inner = result.Unwrap();
        var type = inner.GetType();
        var property = type.GetProperty("ContentType");
        return property?.GetValue(inner) as string;
    }
}
