// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using TodoApp.WinUI3.ViewModels;

namespace TodoApp.WinUI3.Views;

public partial class TodoListWindow : Window
{
    public TodoListWindow()
    {
        this.InitializeComponent();
        this.DataContext = Ioc.Default.GetRequiredService<TodoListViewModel>();
    }

    public TodoListViewModel ViewModel => (TodoListViewModel)DataContext;
}