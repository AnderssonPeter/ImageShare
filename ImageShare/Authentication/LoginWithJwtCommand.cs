using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ImageShare.Authentication;

public sealed record LoginWithJwtCommand(
    [FromRoute] string Token)
    : ICommand<RedirectHttpResult>;
