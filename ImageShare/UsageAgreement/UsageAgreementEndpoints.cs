using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ImageShare.UsageAgreement;

public static class UsageAgreementEndpoints
{
    public static IEndpointRouteBuilder MapUsageAgreementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("usage-agreement").WithTags("usage-agreement");

        group.MapGet("/", GetUsageAgreementAsync)
            .RequireAuthorization()
            .Produces<Ok<UsageAgreementResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/accept", AcceptUsageAgreementAsync)
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<Results<Ok<UsageAgreementResponse>, NotFound>> GetUsageAgreementAsync(
        IMediator mediator,
        [AsParameters] GetUsageAgreementQuery request) =>
        await mediator.Send(request);

    private static async Task<NoContent> AcceptUsageAgreementAsync(
        IMediator mediator,
        [AsParameters] AcceptUsageAgreementCommand request) =>
        await mediator.Send(request);
}
