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
    
    /// <summary>
    /// Gets an see <see cref="IServiceProvider"/> to configure App-Services.
    /// </summary>
    public IServiceProvider Services { get; }

    public App()
    {
        // For the sample we use a SqLite-DB which is in memory only. Therefore, there is no data persistence available.
        // Feel free to adjust the connection string as needed.
        string connectionString = "Data Source=:memory:";

        this.dbConnection = new SqliteConnection(connectionString);

        this.dbConnection.Open();

        // Create the IoC Services provider.
        Services = new ServiceCollection()
            .AddSingleton<TodoListViewModel>()
            .AddScoped<IDbInitializer, DbContextInitializer>()
            .AddDbContext<AppDbContext>(options => options.UseSqlite(this.dbConnection))
            .BuildServiceProvider();
    }

    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <inheritdoc />
    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
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

    /// <summary>
    /// This method initializes the database.
    /// </summary>
    private async void InitializeDatabase()
    {
        using IServiceScope scope = Services.CreateScope();
        IDbInitializer initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        await initializer.InitializeAsync();
    }

    /// <summary>
    /// A helper method for getting a service from the <see cref="Services"/>-collection.
    /// </summary>
    /// <remarks>
    /// You can see this in action in the <see cref="DbContextInitializer"/> class.
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