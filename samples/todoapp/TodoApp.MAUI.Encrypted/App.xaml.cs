// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;
#endif

using Microsoft.EntityFrameworkCore;
using TodoApp.MAUI.Models;
using TodoApp.MAUI.Services;
using TodoApp.MAUI.ViewModels;

namespace TodoApp.MAUI;

public partial class App : Application
{
#if WINDOWS
    const int WindowWidth = 400;
    const int WindowHeight = 800;
#endif

    public App()
    {
        InitializeComponent();

        // The offline store is encrypted on disk.  Resolve the key once here, at the single start-up point,
        // so the first-run generation cannot race (see SecureStorageEncryptionKeyProvider): the key is created
        // on first run, stored in the platform secure store, and reused on every later launch.  SecureStorage
        // is async, so resolve it off the UI thread to avoid a main-thread deadlock when blocking in this
        // synchronous constructor.
        string dbKey = Task.Run(() => new SecureStorageEncryptionKeyProvider().GetOrCreateKeyAsync()).GetAwaiter().GetResult();
        string dbPath = Path.Combine(FileSystem.AppDataDirectory, "todoapp.db");

        Services = new ServiceCollection()
            .AddTransient<MainViewModel>()
            .AddTransient<IAlertService, AlertService>()
            .AddScoped<IDbInitializer, DbContextInitializer>()
            .AddDbContext<AppDbContext>(options => options.UseEncryptedSqlite($"Data Source={dbPath}", dbKey))
            .BuildServiceProvider();

        using (IServiceScope scope = Services.CreateScope())
        {
            IDbInitializer initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
            initializer.Initialize();
        }

        Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(nameof(IWindow), (handler, view) =>
        {
#if WINDOWS
            handler.PlatformView.Activate();

            IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(handler.PlatformView);
            AppWindow appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(windowHandle));
            appWindow.Resize(new SizeInt32(WindowWidth, WindowHeight));
#endif
        });
    }

    public IServiceProvider Services { get; }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}