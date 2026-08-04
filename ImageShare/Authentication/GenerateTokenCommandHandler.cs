using ImageShare.Errors;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ImageShare.Authentication;

internal sealed class GenerateTokenCommandHandler(JwtTokenIssuer tokenIssuer)
    : ICommandHandler<GenerateTokenCommand, Ok<string>>
{
    public ValueTask<Ok<string>> Handle(GenerateTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new BadRequestException("A name must be specified.");
        }

        if (string.IsNullOrWhiteSpace(request.Filter))
        {
            throw new BadRequestException("A filter must be specified.");
        }

        if (request.EndDate <= DateTime.UtcNow)
        {
            throw new BadRequestException("The end date must be in the future.");
        }

        var token = tokenIssuer.CreateToken(request.Name, request.Filter, request.EndDate);
        return new(TypedResults.Ok(token));
    }
}
