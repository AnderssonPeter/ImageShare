using ImageShare.Authentication;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ImageShare.Browsing;

[RequireAuthentication]
public sealed record GetEntriesQuery(
    [FromRoute] RelativePath Path,
    [FromQuery] int Page = 1,
    [FromQuery] int PageSize = 50)
    : IQuery<Ok<PaginatedResult<FolderEntry>>>;
