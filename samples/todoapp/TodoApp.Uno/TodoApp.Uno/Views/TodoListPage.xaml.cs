using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using TodoApp.Uno.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;

namespace TodoApp.Uno.Views;

public sealed partial class TodoListPage : Page
{
    public TodoListViewModel ViewModel => (TodoListViewModel)DataContext!;

    public TodoListPage()
    {
        this.InitializeComponent();

        DataContextChanged += DataContextChangedHandler;
    }



    // on loaded override
    private async void DataContextChangedHandler(object sender, DataContextChangedEventArgs e)
    {
        // Ensure e.NewValue is not null and is a TodoListViewModel (for some reason on iOS it is the ShellViewModel on one of the calls)
        if (e.NewValue != null && e.NewValue is TodoListViewModel && ViewModel.LoadPageCommand.CanExecute(null))
        {
            ViewModel.NotificationHandler += PublishNotification;
            await ViewModel.LoadPageCommand.ExecuteAsync(null);
        }
    }

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

    private void TextBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        // if key is enter, run AddItem command on ViewModel
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            ViewModel.AddItemCommand.Execute(null);
            ViewModel.Title = string.Empty;
        }
    }
}
