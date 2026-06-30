namespace ImageShare.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/", () => Results.Ok(new { App = "ImageShare", Status = "Running" }));
        return endpoints;
    }
}
