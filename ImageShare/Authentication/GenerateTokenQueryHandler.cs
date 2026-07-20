using ImageShare.Errors;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ImageShare.Authentication;

internal sealed class GenerateTokenQueryHandler(JwtTokenService tokenService)
    : IQueryHandler<GenerateTokenQuery, Ok<string>>
{
    public ValueTask<Ok<string>> Handle(GenerateTokenQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Filter))
        {
            throw new BadRequestException("A filter must be specified.");
        }

        if (request.EndDate <= DateTime.UtcNow)
        {
            throw new BadRequestException("The end date must be in the future.");
        }

        var token = tokenService.CreateToken(request.Filter, request.EndDate);
        return new(TypedResults.Ok(token));
    }
}
