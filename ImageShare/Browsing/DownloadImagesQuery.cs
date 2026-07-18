using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace ImageShare.Browsing;

public sealed record DownloadImagesQuery(
    [FromQuery] StringValues Folders,
    [FromQuery] StringValues Format)
    : IQuery<Results<PushStreamHttpResult, UnauthorizedHttpResult, BadRequest, ForbidHttpResult, NotFound>>;
