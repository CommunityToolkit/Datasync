using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TodoApp.Avalonia.Database;
using TodoApp.Avalonia.Services;
using TodoApp.Avalonia.ViewModels;
using TodoApp.Avalonia.Views;

namespace TodoApp.Avalonia;

public partial class App : Application, IDisposable
{
    private readonly SqliteConnection dbConnection;
    public IServiceProvider Services { get; }
    public string Test { get; } = "test";

    public App()
    {
#if DEBUG
        string dbFile = ":memory:";
#else
        string dbFile = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "DataSylc.Avalonia.Sample", 
                        "TodoList.db")";
#endif
        
        this.dbConnection = new SqliteConnection($"Data Source={dbFile}");

        this.dbConnection.Open();

        // Create the IoC Services provider.
        Services = new ServiceCollection()
            .AddSingleton<TodoListViewModel>()
            .AddScoped<IDbInitializer, DbContextInitializer>()
            .AddDbContext<AppDbContext>(options => options.UseSqlite(this.dbConnection))
            .BuildServiceProvider();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);
            desktop.MainWindow = new MainWindow { DataContext = GetRequiredService<TodoListViewModel>() };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView { DataContext = GetRequiredService<TodoListViewModel>() };
        }


        // Initialize the database after DataContext was set
        InitializeDatabase();

        base.OnFrameworkInitializationCompleted();
    }

    private async void InitializeDatabase()
    {
        using IServiceScope scope = Services.CreateScope();
        IDbInitializer initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        await initializer.InitializeAsync();

        Dispatcher.UIThread.Post(
            () => GetRequiredService<TodoListViewModel>().ShowInfoAlert("All data loaded"),
            DispatcherPriority.Loaded);
    }

    /// <summary>
    /// A helper method for getting a service from the services collection.
    /// </summary>
    /// <remarks>
    /// You can see this in action in the <see cref="TodoListPage"/> class.
    /// </remarks>
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