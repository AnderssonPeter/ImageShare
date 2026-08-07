using System.Net.Http.Headers;
using ImageShare.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Mirality.FileProviders;
using Mirality.FileProviders.InMemory;
using TUnit.AspNetCore;

namespace ImageShare.Tests;

public abstract class IntegrationTestBase : WebApplicationTest<ImageShareWebApplicationFactory, Program>
{
    protected InMemoryFileProvider FileProvider { get; private set; } = null!;

    protected override async Task SetupAsync()
    {
        FileProvider = new InMemoryFileProvider();
        await base.SetupAsync();
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.RemoveAll<IFileProvider>();
        services.RemoveAll<IWritableFileProvider>();
        services.RemoveAll<ISyncWritableFileProvider>();
        services.AddSingleton(FileProvider);
        services.AddSingleton<ISyncWritableFileProvider>(FileProvider);
        services.AddSingleton<IWritableFileProvider>(FileProvider);
        services.AddSingleton<IFileProvider>(FileProvider);
    }

    protected HttpClient CreateClientWithApiKey()
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Add(AuthenticationExtensions.ApiKeyHeaderName, ImageShareWebApplicationFactory.TestApiKey);
        return client;
    }
}
