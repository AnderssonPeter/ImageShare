using System.Text.Json.Serialization;
using ImageShare.Authentication;
using ImageShare.Browsing;
using Microsoft.Extensions.Primitives;

namespace ImageShare;

[JsonSerializable(typeof(IUser))]
[JsonSerializable(typeof(FolderEntry))]
[JsonSerializable(typeof(FolderEntry[]))]
[JsonSerializable(typeof(PaginatedResult<FolderEntry>))]
[JsonSerializable(typeof(StorageOptions))]
[JsonSerializable(typeof(StringValues))]
[JsonSerializable(typeof(RelativePath))]
// register ALL types of serialisable DTOs
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
