using ImageShare.Errors;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace ImageShare.UsageAgreement;

internal sealed class GetUsageAgreementQueryHandler(
    IOptions<UsageAgreementOptions> options,
    IUsageAgreement usageAgreement)
    : IQueryHandler<GetUsageAgreementQuery, Ok<UsageAgreementResponse>>
{
    public ValueTask<Ok<UsageAgreementResponse>> Handle(
        GetUsageAgreementQuery request,
        CancellationToken cancellationToken)
    {
        var match = options.Value.FindBestMatch(request.AcceptLanguage);
        if (match is null)
        {
            throw new NotFoundException("No usage agreement is configured.");
        }

        return new(TypedResults.Ok(new UsageAgreementResponse(match.Language, match.Text, usageAgreement.IsAccepted)));
    }
}
