using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ImageShare.Browsing;

public sealed record ServeImageQuery(
    [FromRoute] string Path,
    [FromHeader(Name = "Accept")] string Accept = "",
    [FromQuery] bool Thumbnail = false)
    : IQuery<Results<FileStreamHttpResult, UnauthorizedHttpResult, BadRequest, ForbidHttpResult, NotFound, StatusCodeHttpResult>>;
