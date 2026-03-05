namespace BackBase.API.IntegrationTests.Fixtures;

using BackBase.Application.Constants;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

public abstract class SignalRTestBase : IntegrationTestBase
{
    private static int _userCounter;
    private readonly List<HubConnection> _connections = [];

    protected SignalRTestBase(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    public override async Task DisposeAsync()
    {
        foreach (var connection in _connections)
        {
            if (connection.State != HubConnectionState.Disconnected)
            {
                await connection.StopAsync();
            }

            await connection.DisposeAsync();
        }

        _connections.Clear();
        await base.DisposeAsync();
    }

    protected HubConnection CreateHubConnection(string? accessToken = null)
    {
        var hubUrl = new Uri(Factory.Server.BaseAddress, ChatConstants.HubPath);

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();

                if (accessToken is not null)
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                }
            })
            .Build();

        _connections.Add(connection);
        return connection;
    }

    protected async Task<(string AccessToken, string Email)> RegisterAndLoginAsync()
    {
        var counter = Interlocked.Increment(ref _userCounter);
        var email = $"signalr_test_{counter}_{Guid.NewGuid():N}@test.com";
        const string password = "TestPassword1";

        await RegisterUserAsync(email, password);
        var loginResponse = await LoginUserAsync(email, password);

        return (loginResponse.AccessToken, email);
    }
}
