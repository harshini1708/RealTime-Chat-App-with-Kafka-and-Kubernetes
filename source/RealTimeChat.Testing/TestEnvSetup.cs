using System.Reflection;
using Common.Testing.Logging;
using NUnit.Framework;
using Serilog;
using Serilog.Events;

namespace CecoChat.Testing;

public static class TestEnvSetup
{
    public static void InitEnv()
    {
        const string envName = "ASPNETCORE_ENVIRONMENT";
        string? env = Environment.GetEnvironmentVariable(envName);
        if (string.IsNullOrWhiteSpace(env))
        {
            env = "Development";
            Environment.SetEnvironmentVariable(envName, env);
        }

        TestContext.Progress.WriteLine($"{envName}: {env}");
    }

    public static void ConfigureLogging()
    {
        const string outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} | {Message:lj}{NewLine}{Exception}";

        Assembly testAssembly = Assembly.GetCallingAssembly();
        string name = testAssembly.GetName().Name!;
        string binPath = Path.GetDirectoryName(testAssembly.Location) ?? Environment.CurrentDirectory;
        // going from /source/project/bin/debug/.netX.Y/ to /source/logs/project.txt
        string filePath = Path.Combine(binPath, "..", "..", "..", "..", "logs", $"{name}.txt");

        LoggerConfiguration loggerConfig = new();

        loggerConfig
            .MinimumLevel.Is(LogEventLevel.Information)
            .MinimumLevel.Override("CecoChat", LogEventLevel.Verbose)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
            .MinimumLevel.Override("Grpc", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Destructure.ToMaximumDepth(8)
            .Destructure.ToMaximumStringLength(1024)
            .Destructure.ToMaximumCollectionCount(32)
            .WriteTo.Debug(
                outputTemplate: outputTemplate)
            .WriteTo.NUnit(
                outputTemplate: outputTemplate)
            .WriteTo.File(
                outputTemplate: outputTemplate,
                path: filePath,
                rollingInterval: RollingInterval.Day);

        Log.Logger = loggerConfig.CreateLogger();
    }
}
