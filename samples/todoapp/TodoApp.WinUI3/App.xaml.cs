// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using TodoApp.WinUI3.Database;
using TodoApp.WinUI3.ViewModels;

namespace TodoApp.WinUI3;

public partial class App : Application
{
    private Window m_window;

    public App()
    {
        InitializeComponent();
        RegisterServices();
    }

    private void RegisterServices()
    {
        var services = new ServiceCollection()
            .AddTransient<TodoListViewModel>()
            .AddDbContext<AppDbContext>()
            .BuildServiceProvider();

        // Registers the IoC container and services.
        Ioc.Default.ConfigureServices(services);


    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        this.m_window = new MainWindow();
        this.m_window.Activate();
    }
}
