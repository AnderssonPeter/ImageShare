using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ImageShare.Authentication;

[RequireAuthentication]
[RequireAdmin]
public sealed record GenerateTokenQuery(
    [FromQuery] string Name,
    [FromQuery] string Filter,
    [FromQuery] DateTime EndDate)
    : IQuery<Ok<string>>;
