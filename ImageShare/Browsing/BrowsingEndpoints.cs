using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ImageShare.Browsing;

public static class BrowsingEndpoints
{
    public static IEndpointRouteBuilder MapFolderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/folders").RequireAuthorization();

        group.MapGet("/", async (IMediator mediator, [FromQuery] int Page = 1, [FromQuery] int PageSize = 50) =>
            await mediator.Send(new GetEntriesQuery(RelativePath.Root, Page, PageSize)))
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{**path}", async (IMediator mediator, [AsParameters] GetEntriesQuery request) =>
            await mediator.Send(request))
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
