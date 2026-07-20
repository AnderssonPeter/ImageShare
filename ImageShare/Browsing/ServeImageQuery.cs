using ImageShare.Authentication;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ImageShare.Browsing;

[RequireAuthentication]
public sealed record ServeImageQuery(
    [FromRoute] RelativePath Path,
    [FromHeader(Name = "Accept")] string Accept = "",
    [FromQuery] bool Thumbnail = false)
    : IQuery<FileStreamHttpResult>;
