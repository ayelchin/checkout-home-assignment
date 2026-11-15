using System.Net;

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace PaymentGateway.Api.IntegrationTests.Infrastructure;

public sealed class BankSimulatorFixture : IAsyncLifetime
{
    private IContainer? _container;
    public Uri Address => new($"http://{_container!.Hostname}:{_container.GetMappedPublicPort(8080)}");

    public async Task InitializeAsync()
    {
        try
        {
            _container = new ContainerBuilder()
                .WithImage("bbyars/mountebank:2.8.1")
                .WithPortBinding(8080, true)
                .WithResourceMapping(new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "imposters")), "/imposters")
                .WithCommand("--configfile", "/imposters/bank_simulator.ejs", "--allowInjection")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request
                    .ForPort(8080).ForPath("/payments").ForStatusCode(HttpStatusCode.BadRequest)))
                .Build();
            using CancellationTokenSource startup = new(TimeSpan.FromMinutes(2));
            await _container.StartAsync(startup.Token);
        }
        catch (Exception exception)
        {
            if (_container is not null)
            {
                await _container.DisposeAsync();
                _container = null;
            }
            throw new InvalidOperationException("Bank simulator could not start. Start Docker Desktop with Linux containers and ensure bbyars/mountebank:2.8.1 can be pulled.", exception);
        }
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}