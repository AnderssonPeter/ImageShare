using ImageShare;
using ImageShare.Authentication;
using ImageShare.Endpoints;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default));
builder.Services.AddOpenIdConnectAuthentication(builder.Configuration);
builder.Services.AddUser();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();
app.MapWeatherForecastEndpoints();
app.MapAuthEndpoints();

await app.RunAsync();
