// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using TodoApp.WinUI3.ViewModels;

namespace TodoApp.WinUI3.Views;

public sealed partial class TodoListPage : Page
{
    public TodoListPage()
    {
        InitializeComponent();
        DataContext = App.GetRequiredService<TodoListViewModel>();
        ViewModel.NotificationHandler += PublishNotification;

        // Event handler responses
        Loaded += InvokeLoadPage;
    }

    public TodoListViewModel ViewModel => (TodoListViewModel)DataContext!;

    internal void PublishNotification(object sender, NotificationEventArgs args)
    {
        Notification notification = new()
        {
            Title = args.Title,
            Message = args.Message,
            Severity = args.IsError ? InfoBarSeverity.Error : InfoBarSeverity.Informational,
            Duration = args.IsError ? null : TimeSpan.FromSeconds(2)
        };
        _ = this.NotificationQueue.Show(notification);
    }

    private async void InvokeLoadPage(object sender, RoutedEventArgs e)
    {
        if (ViewModel.LoadPageCommand.CanExecute(null))
        {
            await ViewModel.LoadPageCommand.ExecuteAsync(null);
        }
    }

    private async void TitleTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            if (ViewModel.AddItemCommand.CanExecute(null))
            {
                await ViewModel.AddItemCommand.ExecuteAsync(null);
            }
        }
    }
}
