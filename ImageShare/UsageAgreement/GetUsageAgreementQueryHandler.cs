using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageShare.UsageAgreement;

internal sealed class GetUsageAgreementQueryHandler(
    IOptions<UsageAgreementOptions> options,
    IUsageAgreement usageAgreement,
    ILogger<GetUsageAgreementQueryHandler> logger)
    : IQueryHandler<GetUsageAgreementQuery, Results<Ok<UsageAgreementResponse>, NotFound>>
{
    public ValueTask<Results<Ok<UsageAgreementResponse>, NotFound>> Handle(
        GetUsageAgreementQuery request,
        CancellationToken cancellationToken)
    {
        var match = options.Value.FindBestMatch(request.AcceptLanguage);
        if (match is null)
        {
            logger.LogDebug("No usage agreement is configured; returning 404.");
            return new(TypedResults.NotFound());
        }

        return new(TypedResults.Ok(new UsageAgreementResponse(match.Language, match.Text, usageAgreement.IsAccepted)));
    }
}
