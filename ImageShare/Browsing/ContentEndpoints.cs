using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ImageShare.Browsing;

public static class ContentEndpoints
{
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/content").RequireAuthorization().WithTags("content");

        group.MapGet("/", GetContentAsync)
            .Produces<Ok<PaginatedResult<FolderEntry>>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{**path}", GetContentByPathAsync)
            .Produces<Ok<PaginatedResult<FolderEntry>>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/download/{**folder}", DownloadImagesAsync)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/random", GetRandomImageAsync)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status406NotAcceptable);

        group.MapGet("/random/{**folder}", GetRandomImageByFolderAsync)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status406NotAcceptable);

        group.MapGet("/image/{**path}", ServeImageAsync)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status406NotAcceptable);

        return endpoints;
    }

    private static async Task<Ok<PaginatedResult<FolderEntry>>> GetContentAsync(
        IMediator mediator,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50) =>
        await mediator.Send(new GetEntriesQuery(RelativePath.Root, page, pageSize));

    private static async Task<Ok<PaginatedResult<FolderEntry>>> GetContentByPathAsync(
        IMediator mediator,
        [AsParameters] GetEntriesQuery request) =>
        await mediator.Send(request);

    private static async Task<PushStreamHttpResult> DownloadImagesAsync(IMediator mediator, [AsParameters] DownloadImagesQuery request) =>
        await mediator.Send(request);

    private static async Task<FileStreamHttpResult> GetRandomImageAsync(
        IMediator mediator,
        [FromQuery] bool thumbnail = false,
        [FromHeader(Name = "Accept")] string accept = "") =>
        await mediator.Send(new GetRandomImageQuery(RelativePath.Root, thumbnail, true, accept));

    private static async Task<FileStreamHttpResult> GetRandomImageByFolderAsync(IMediator mediator, [AsParameters] GetRandomImageQuery request) =>
        await mediator.Send(request);

    private static async Task<FileStreamHttpResult> ServeImageAsync(IMediator mediator, [AsParameters] ServeImageQuery request) =>
        await mediator.Send(request);
}
