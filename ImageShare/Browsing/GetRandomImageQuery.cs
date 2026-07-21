using ImageShare.Authentication;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ImageShare.Browsing;

[RequireAuthentication]
public sealed record GetRandomImageQuery(
    [FromRoute] RelativePath Folder,
    [FromQuery] bool Thumbnail = false,
    [FromQuery] bool Recursive = false,
    [FromHeader(Name = "Accept")] string Accept = "")
    : IQuery<FileStreamHttpResult>;
