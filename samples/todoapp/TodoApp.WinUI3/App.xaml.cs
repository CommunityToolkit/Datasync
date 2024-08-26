// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using TodoApp.WinUI3.Database;
using TodoApp.WinUI3.ViewModels;
using TodoApp.WinUI3.Views;

namespace TodoApp.WinUI3;

public partial class App : Application, IDisposable
{
    private Window m_window;
    private Frame m_frame;
    private readonly SqliteConnection dbConnection;

    public App()
    {
        InitializeComponent();

        this.dbConnection = new SqliteConnection("Data Source=:memory:");
        this.dbConnection.Open();

        IServiceCollection services = new ServiceCollection()
            .AddTransient<TodoListViewModel>()
            .AddScoped<IDbInitializer, DbContextInitializer>()
            .AddDbContext<AppDbContext>(options => options.UseSqlite(this.dbConnection));

        Ioc.Default.ConfigureServices(services.BuildServiceProvider());

        // Initialize the database
        using (IServiceScope scope = Ioc.Default.CreateScope())
        {
            IDbInitializer initializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
            initializer.Initialize();
        }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        this.m_window = new Window();
        this.m_frame = new Frame();
        this.m_window.Content = this.m_frame;
        this.m_window.Activate();
        _ = this.m_frame.Navigate(typeof(TodoListPage));
    }

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
