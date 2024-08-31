// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using TodoApp.WPF.Database;
using TodoApp.WPF.Services;
using TodoApp.WPF.ViewModels;

namespace TodoApp.WPF;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application, IDisposable
{
    private readonly SqliteConnection dbConnection;
    public IServiceProvider Services { get; }

    public App()
    {
        this.dbConnection = new SqliteConnection("Data Source=:memory:");
        this.dbConnection.Open();

        // Create the IoC Services provider.
        Services = new ServiceCollection()
            .AddTransient<TodoListViewModel>()
            .AddScoped<IDbInitializer, DbContextInitializer>()
            .AddScoped<IAlertService, AlertService>()
            .AddDbContext<AppDbContext>(options => options.UseSqlite(this.dbConnection))
            .BuildServiceProvider();

        // Initialize the database
        InitializeDatabase();
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
    /// <remarks>
    /// You can see this in action in the <see cref="TodoListPage"/> class.
    /// </remarks>
    /// <typeparam name="TService">The type of the service.</typeparam>
    /// <returns>An instance of the service</returns>
    public static TService GetRequiredService<TService>() where TService : notnull
        => ((App)App.Current).Services.GetRequiredService<TService>();

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

