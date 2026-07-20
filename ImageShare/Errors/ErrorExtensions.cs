using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ImageShare.Errors;

public static class ErrorExtensions
{
    public static IServiceCollection AddCustomErrors(this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                if (context.Exception is not null)
                {
                    context.ProblemDetails.Detail = context.Exception.Message;
                }

                context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
            };
        });
        return services;
    }

    public static IApplicationBuilder UseCustomErrors(this IApplicationBuilder app) =>
        app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            StatusCodeSelector = SelectStatusCode,
        });

    internal static int SelectStatusCode(Exception exception) => exception switch
    {
        NotAuthenticatedException => StatusCodes.Status401Unauthorized,
        BadRequestException => StatusCodes.Status400BadRequest,
        ForbiddenException => StatusCodes.Status403Forbidden,
        FolderAccessDeniedException => StatusCodes.Status403Forbidden,
        NotFoundException => StatusCodes.Status404NotFound,
        NotAcceptableException => StatusCodes.Status406NotAcceptable,
        _ => StatusCodes.Status500InternalServerError,
    };
}
