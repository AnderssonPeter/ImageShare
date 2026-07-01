using System.Text.Json.Serialization;
using ImageShare.Authentication;
using ImageShare.Endpoints;

namespace ImageShare;

[JsonSerializable(typeof(WeatherForecast))]
[JsonSerializable(typeof(WeatherForecast[]))]
[JsonSerializable(typeof(IUser))]
// register ALL types of serialisable DTOs
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
