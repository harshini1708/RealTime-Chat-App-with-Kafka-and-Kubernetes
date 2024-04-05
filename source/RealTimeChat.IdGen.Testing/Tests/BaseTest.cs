using NUnit.Framework;

namespace CecoChat.IdGen.Testing.Tests;

public abstract class BaseTest
{
    private IdGenService _idGenService;
    private IdGenClient _idGenClient;

    [OneTimeSetUp]
    public async Task BeforeAllTests()
    {
        _idGenService = new(
            environment: "Test",
            listenPort: 32002,
            certificatePath: "services.pfx",
            certificatePassword: "cecochat",
            configFilePath: "idgen-service.json");
        await _idGenService.Run();

        _idGenClient = new IdGenClient(
            configFilePath: "idgen-client.json");
    }

    [OneTimeTearDown]
    public async Task AfterAllTests()
    {
        _idGenClient.Dispose();
        await _idGenService.DisposeAsync();
    }

    protected IdGenClient Client => _idGenClient;
}
