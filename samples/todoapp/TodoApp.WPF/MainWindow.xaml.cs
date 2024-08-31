// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using TodoApp.WPF.ViewModels;

namespace TodoApp.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly TodoListViewModel viewModel;

    public MainWindow()
    {
        InitializeComponent();
        this.viewModel = App.GetRequiredService<TodoListViewModel>();
        DataContext = this.viewModel;
    }
}