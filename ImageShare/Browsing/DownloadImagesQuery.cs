using ImageShare.Authentication;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ImageShare.Browsing;

[RequireAuthentication]
public sealed record DownloadImagesQuery(
    [FromRoute] RelativePath Folder,
    [FromQuery] string[] Format = default!)
    : IQuery<PushStreamHttpResult>;
