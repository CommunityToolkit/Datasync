// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using TodoApp.Uno.ViewModels;
#if WINDOWS
//using CommunityToolkit.WinUI.Behaviors;
#endif


namespace TodoApp.Uno.Views;

public sealed partial class TodoListPage : Page
{
    public TodoListPage()
    {
        InitializeComponent();
        DataContext = App.GetRequiredService<TodoListViewModel>();
        //ViewModel.NotificationHandler += PublishNotification;
    }

    public TodoListViewModel ViewModel => (TodoListViewModel)DataContext!;

    internal void PublishNotification(object sender, NotificationEventArgs args)
    {
        //#if WINDOWS
        //        Notification notification = new()
        //        {
        //            Title = args.Title,
        //            Message = args.Message,
        //            Severity = args.IsError ? InfoBarSeverity.Error : InfoBarSeverity.Informational,
        //            Duration = args.IsError ? null : TimeSpan.FromSeconds(2)
        //        };

        //        _ = NotificationQueue.Show(notification);
        //#endif
    }
}
