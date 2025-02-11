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

public partial class App : Application, IDisposable
{
#if WINDOWS
    const int WindowWidth = 400;
    const int WindowHeight = 800;
#endif

    public App()
    {
        InitializeComponent();

        Services = new ServiceCollection()
            .AddTransient<MainViewModel>()
            .AddTransient<IAlertService, AlertService>()
            .AddScoped<IDbInitializer, DbContextInitializer>()
            .AddDbContext<AppDbContext>(options => options.UseSqlite(SqliteHelper.Connection))
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

    #region IDisposable
    private bool hasDisposed;

    protected virtual void Dispose(bool disposing)
    {
        if (!this.hasDisposed)
        {
            if (disposing)
            {
                SqliteHelper.Connection.Dispose();
            }

            this.hasDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion
}