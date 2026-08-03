using Microsoft.AspNetCore.Http.HttpResults;

namespace ImageShare.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", GetHealth);
        return endpoints;
    }

    private static Ok<string> GetHealth() => TypedResults.Ok("pong");
}
