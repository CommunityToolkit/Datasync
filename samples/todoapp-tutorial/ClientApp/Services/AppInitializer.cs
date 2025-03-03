using ClientApp.Interfaces;

namespace ClientApp.Services;

public class AppInitializer : IAppInitializer
{
    public Task Initialize()
    {
        return Task.CompletedTask;
    }
}