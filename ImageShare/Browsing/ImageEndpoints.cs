using Mediator;
using Microsoft.AspNetCore.Http;

namespace ImageShare.Browsing;

public static class ImageEndpoints
{
    public static IEndpointRouteBuilder MapImageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/images").RequireAuthorization();

        group.MapGet("/download", async (IMediator mediator, [AsParameters] DownloadImagesQuery request) =>
            await mediator.Send(request));

        group.MapGet("/random", async (IMediator mediator, [AsParameters] GetRandomImageQuery request) =>
            await mediator.Send(request));

        group.MapGet("/{**path}", async (IMediator mediator, [AsParameters] ServeImageQuery request) =>
            await mediator.Send(request));

        return endpoints;
    }
}
