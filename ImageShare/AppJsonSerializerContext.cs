using System.Text.Json.Serialization;
using ImageShare.Authentication;
using ImageShare.Browsing;
using ImageShare.UsageAgreement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

namespace ImageShare;

[JsonSerializable(typeof(IUser))]
[JsonSerializable(typeof(FolderEntry))]
[JsonSerializable(typeof(FolderEntry[]))]
[JsonSerializable(typeof(IReadOnlyList<FolderEntry>))]
[JsonSerializable(typeof(StorageOptions))]
[JsonSerializable(typeof(StringValues))]
[JsonSerializable(typeof(RelativePath))]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(UsageAgreementResponse))]
// register ALL types of serialisable DTOs
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
