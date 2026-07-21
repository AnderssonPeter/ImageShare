using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ImageShare.Browsing;

public static class ContentEndpoints
{
    public static IEndpointRouteBuilder MapContentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/content").RequireAuthorization();

        group.MapGet("/", async (IMediator mediator, [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
            await mediator.Send(new GetEntriesQuery(RelativePath.Root, page, pageSize)))
            .Produces<Ok<PaginatedResult<FolderEntry>>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{**path}", async (IMediator mediator, [AsParameters] GetEntriesQuery request) =>
            await mediator.Send(request))
            .Produces<Ok<PaginatedResult<FolderEntry>>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/download/{**folder}", async (IMediator mediator, [AsParameters] DownloadImagesQuery request) =>
            await mediator.Send(request))
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/random/{**folder}", async (IMediator mediator, [AsParameters] GetRandomImageQuery request) =>
            await mediator.Send(request))
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status406NotAcceptable);

        group.MapGet("/image/{**path}", async (IMediator mediator, [AsParameters] ServeImageQuery request) =>
            await mediator.Send(request))
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status406NotAcceptable);

        return endpoints;
    }
}
