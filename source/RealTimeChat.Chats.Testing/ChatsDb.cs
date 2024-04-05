using Common.Net;
using Common.Testing.Logging;
using Common.Testing.TestContainers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using NUnit.Framework;

namespace CecoChat.Chats.Testing;

public interface IChatsDb : IAsyncDisposable
{
    public string Host { get; }

    public int Port { get; }

    public Task Start(TimeSpan timeout);

    public Task PrintLogs();
}

/// <summary>
/// Provides access to a manually started local instance of Cassandra.
/// Used when running tests locally during development.
/// </summary>
public sealed class ExistingChatsDb : IChatsDb
{
    public ExistingChatsDb()
    {
        TestContext.Progress.WriteLine($"Using an existing Chats DB @{Host}:{Port}");
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public string Host => "localhost";

    public int Port => 9042;

    public Task Start(TimeSpan timeout) => Task.CompletedTask;

    public Task PrintLogs() => Task.CompletedTask;
}

/// <summary>
/// Starts a Cassandra container using Testcontainers package.
/// Used during the CI pipeline.
/// </summary>
public sealed class TestContainersChatsDb : IChatsDb
{
    private readonly int _cassandraHostPort;
    private readonly IContainer _cassandra;

    public TestContainersChatsDb(INetwork dockerNetwork, string name, string cluster, string seeds, string localDc)
    {
        _cassandraHostPort = NetworkUtils.GetNextFreeTcpPort();
        TestContext.Progress.WriteLine($"Setting Cassandra host port to {_cassandraHostPort}");
        const int cassandraContainerPort = 9042;

        _cassandra = new ContainerBuilder()
            .WithImage("cassandra:4.1.3")
            .WithName($"cecochat-test-{name}")
            .WithHostname(name)
            .WithNetwork(dockerNetwork)
            .WithPortBinding(_cassandraHostPort, cassandraContainerPort)
            .WithEnvironment(new Dictionary<string, string>
            {
                { "CASSANDRA_SEEDS", seeds },
                { "CASSANDRA_CLUSTER_NAME", cluster },
                { "CASSANDRA_DC", localDc },
                { "CASSANDRA_RACK", "Rack0" },
                { "CASSANDRA_ENDPOINT_SNITCH", "GossipingPropertyFileSnitch" },
                { "CASSANDRA_NUM_TOKENS", "128" },
                { "HEAP_NEWSIZE", "128M" },
                { "MAX_HEAP_SIZE", "512M" }
            })
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilCassandraQueryExecuted(_cassandraHostPort, localDc, showErrorInterval: TimeSpan.FromSeconds(10)))
            .WithLogger(new NUnitProgressLogger<TestContainersChatsDb>())
            .Build();

        TestContext.Progress.WriteLine($"Using a Testcontainers Chats DB @{_cassandra.Hostname}:{_cassandraHostPort}");
    }

    public async ValueTask DisposeAsync()
    {
        await _cassandra.DisposeAsync();
    }

    public string Host => _cassandra.Hostname;

    public int Port => _cassandraHostPort;

    public async Task Start(TimeSpan timeout)
    {
        using CancellationTokenSource timeoutCts = new(timeout);
        await _cassandra.StartAsync(timeoutCts.Token);
    }

    public async Task PrintLogs()
    {
        (string stdout, string stderr) logs = await _cassandra.GetLogsAsync();

        await TestContext.Progress.WriteLineAsync($"{_cassandra.Hostname} stdout:");
        await TestContext.Progress.WriteLineAsync(logs.stdout);
        await TestContext.Progress.WriteLineAsync($"{_cassandra.Hostname} stderr:");
        await TestContext.Progress.WriteLineAsync(logs.stderr);
    }
}
