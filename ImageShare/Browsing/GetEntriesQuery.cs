using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace ImageShare.Browsing;

public sealed record GetEntriesQuery(
    [FromRoute] string Path,
    [FromQuery] int Page = 1,
    [FromQuery] int PageSize = 50)
    : IQuery<Results<Ok<PaginatedResult<FolderEntry>>, UnauthorizedHttpResult, BadRequest, NotFound>>;
