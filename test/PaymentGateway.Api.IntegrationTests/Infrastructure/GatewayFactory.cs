using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;

using PaymentGateway.Api.Controllers;

namespace PaymentGateway.Api.IntegrationTests.Infrastructure;

public sealed class GatewayFactory(Uri? bankAddress = null, FakeTimeProvider? clock = null) : WebApplicationFactory<PaymentsController>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Merchants:ApiKeys:merchant-one-key"] = "merchant-one",
            ["Merchants:ApiKeys:merchant-two-key"] = "merchant-two",
            ["Bank:BaseUrl"] = (bankAddress ?? new Uri("http://127.0.0.1:1")).ToString(),
            ["Bank:ResponseTimeoutSeconds"] = "5"
        }));
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.AddSingleton<TimeProvider>(clock ?? new FakeTimeProvider(new DateTimeOffset(2030, 11, 15, 0, 0, 0, TimeSpan.Zero)));
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
        });
    }

    public HttpClient CreateMerchantClient(string apiKey = "merchant-one-key")
    {
        HttpClient client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        return client;
    }
}