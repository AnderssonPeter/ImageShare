using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ImageShare.Authentication;

[RequireAuthentication]
[RequireAdmin]
public sealed record GenerateTokenCommand(
    [FromRoute] string Name,
    [FromRoute] string Filter,
    [FromRoute] DateTime EndDate)
    : ICommand<Ok<string>>;
