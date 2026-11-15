using System.Collections.Concurrent;
using System.Text.Json.Nodes;

using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace PaymentGateway.Api.IntegrationTests.Infrastructure;

internal sealed class BankStub : IAsyncDisposable
{
    private readonly WebApplication _application;
    public Uri Address { get; }
    public ConcurrentQueue<JsonNode> Requests { get; } = new();

    private BankStub(WebApplication application)
    {
        _application = application;
        Address = new Uri(application.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single());
    }

    public static async Task<BankStub> StartAsync(Func<HttpContext, Task>? response = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        WebApplication application = builder.Build();
        BankStub? stub = null;
        application.MapPost("/payments", async context =>
        {
            stub!.Requests.Enqueue((await JsonNode.ParseAsync(context.Request.Body))!);
            if (response is null)
            {
                await context.Response.WriteAsJsonAsync(new { authorized = true, authorization_code = "approval" });
            }
            else
            {
                await response(context);
            }
        });
        await application.StartAsync();
        stub = new BankStub(application);
        return stub;
    }

    public Task StopAsync() => _application.StopAsync();

    public ValueTask DisposeAsync() => _application.DisposeAsync();
}