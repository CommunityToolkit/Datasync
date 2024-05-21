// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TodoApp.WinUI3.Database;
using TodoApp.WinUI3.Services;
using TodoApp.WinUI3.ViewModels;
using TodoApp.WinUI3.Views;

namespace TodoApp.WinUI3;

public partial class App : Application
{
    private Window m_window;
    private Frame m_frame;

    public App()
    {
        InitializeComponent();

        IServiceCollection services = new ServiceCollection()
            .AddSingleton<ITodoService, LocalTodoService>()
            .AddTransient<TodoListViewModel>()
            .AddDbContext<AppDbContext>();

        Ioc.Default.ConfigureServices(services.BuildServiceProvider());
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        this.m_window = new Window();
        this.m_frame = new Frame();
        this.m_window.Content = this.m_frame;
        this.m_window.Activate();
        _ = this.m_frame.Navigate(typeof(TodoListPage));
    }
}
