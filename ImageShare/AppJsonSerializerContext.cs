using System.Text.Json.Serialization;
using ImageShare.Authentication;
using ImageShare.Browsing;

namespace ImageShare;

[JsonSerializable(typeof(IUser))]
[JsonSerializable(typeof(FolderEntry))]
[JsonSerializable(typeof(FolderEntry[]))]
[JsonSerializable(typeof(PaginatedResult<FolderEntry>))]
[JsonSerializable(typeof(StorageOptions))]
// register ALL types of serialisable DTOs
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
