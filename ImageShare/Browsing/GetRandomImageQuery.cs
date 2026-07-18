using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace ImageShare.Browsing;

public sealed record GetRandomImageQuery(
    [FromQuery] StringValues Folders,
    [FromQuery] bool Thumbnail = false,
    [FromQuery] bool Recursive = false,
    [FromHeader(Name = "Accept")] string Accept = "")
    : IQuery<Results<FileStreamHttpResult, UnauthorizedHttpResult, BadRequest, ForbidHttpResult, NotFound, StatusCodeHttpResult>>;
