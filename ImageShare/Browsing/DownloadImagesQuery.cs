using ImageShare.Authentication;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ImageShare.Browsing;

[RequireAuthentication]
public sealed record DownloadImagesQuery(
    [FromQuery] string[] Folders,
    [FromQuery] string[] Format = default!)
    : IQuery<PushStreamHttpResult>;
