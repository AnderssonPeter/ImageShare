using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ImageShare.UsageAgreement;

internal sealed class AcceptUsageAgreementCommandHandler(IUsageAgreement usageAgreement)
    : ICommandHandler<AcceptUsageAgreementCommand, NoContent>
{
    public ValueTask<NoContent> Handle(AcceptUsageAgreementCommand request, CancellationToken cancellationToken)
    {
        usageAgreement.Accept();
        return new(TypedResults.NoContent());
    }
}
