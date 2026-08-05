using ImageShare.Authentication;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ImageShare.UsageAgreement;

[RequireAuthentication]
public sealed record AcceptUsageAgreementCommand(
    [FromHeader(Name = "Accept-Language")] string AcceptLanguage = "")
    : ICommand<NoContent>;
