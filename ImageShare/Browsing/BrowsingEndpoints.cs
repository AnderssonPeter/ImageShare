using Mediator;
using Microsoft.AspNetCore.Http;

namespace ImageShare.Browsing;

public static class BrowsingEndpoints
{
    public static IEndpointRouteBuilder MapFolderEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/folders").RequireAuthorization();

        group.MapGet("/", async (IMediator mediator, [AsParameters] GetEntriesQuery request) =>
            await mediator.Send(request));

        group.MapGet("/{**path}", async (IMediator mediator, [AsParameters] GetEntriesQuery request) =>
            await mediator.Send(request));

        return endpoints;
    }
}
