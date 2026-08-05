using ImageShare.Authentication;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ImageShare.UsageAgreement;

[RequireAuthentication]
public sealed record GetUsageAgreementQuery(
    [FromHeader(Name = "Accept-Language")] string AcceptLanguage = "")
    : IQuery<Ok<UsageAgreementResponse>>;
