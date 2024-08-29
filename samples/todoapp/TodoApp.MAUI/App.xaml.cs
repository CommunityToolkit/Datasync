// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if WINDOWS
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Windows.Graphics;
#endif

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TodoApp.MAUI.Models;
using TodoApp.MAUI.ViewModels;
using TodoApp.MAUI.Services;

namespace TodoApp.MAUI;

public partial class App : Application, IDisposable
{
#if WINDOWS
    const int WindowWidth = 400;
    const int WindowHeight = 800;
#endif

    private readonly SqliteConnection dbConnection;

    public IServiceProvider Services { get; }

    public App()
    {
        InitializeComponent();

        this.dbConnection = new SqliteConnection("Data Source=:memory:");
        this.dbConnection.Open();

        Services = new ServiceCollection()
            .AddTransient<MainViewModel>()
            .AddTransient<IAlertService, AlertService>()
            .AddScoped<IDbInitializer, DbContextInitializer>()
            .AddDbContext<AppDbContext>(options => options.UseSqlite(this.dbConnection))
            .BuildServiceProvider();

        InitializeDatabase();

        Microsoft.Maui.Handlers.WindowHandler.Mapper.AppendToMapping(nameof(IWindow), (handler, view) =>
        {
#if WINDOWS
            handler.PlatformView.Activate();

            IntPtr windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(handler.PlatformView);
            AppWindow appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(windowHandle));
            appWindow.Resize(new SizeInt32(WindowWidth, WindowHeight));
#endif
        });
        MainPage = new NavigationPage(new MainPage());
    }

    private void InitializeDatabase()
    {
        using IServiceScope scope = Services.CreateScope();
        IDbInitializer initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        initializer.Initialize();
    }

    /// <summary>
    /// A helper method for getting a service from the services collection.
    /// </summary>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <returns>An instance of the service</returns>
    public static TService GetRequiredService<TService>() where TService : notnull
        => ((App)App.Current!).Services.GetRequiredService<TService>();

    #region IDisposable
    private bool hasDisposed;

    protected virtual void Dispose(bool disposing)
    {
        if (!this.hasDisposed)
        {
            if (disposing)
            {
                this.dbConnection.Close();
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
