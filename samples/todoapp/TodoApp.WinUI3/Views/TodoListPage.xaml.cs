// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using TodoApp.WinUI3.ViewModels;

namespace TodoApp.WinUI3.Views;

public sealed partial class TodoListPage : Page
{
    public TodoListPage()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<TodoListViewModel>();
    }

    public TodoListViewModel ViewModel => (TodoListViewModel)DataContext!;
}
