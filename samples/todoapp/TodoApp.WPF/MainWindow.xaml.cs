// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows;
using System.Windows.Input;
using TodoApp.WPF.ViewModels;

namespace TodoApp.WPF;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.GetRequiredService<TodoListViewModel>();
    }

    protected async void TextboxKeyDownHandler(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Return or Key.Enter)
        {
            await ((TodoListViewModel)DataContext).AddItemAsync();
        }
    }
}