// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using TodoApp.MAUI.Models;
using TodoApp.MAUI.Utils;
using TodoApp.MAUI.ViewModels;

namespace TodoApp.MAUI;

public partial class MainPage : ContentPage, IMVVMHelper
{
    private readonly MainViewModel _viewModel;

    public MainPage()
    {
        InitializeComponent();
        this._viewModel = ServicesContainer.GetRequiredService<MainViewModel>();
        this._viewModel.MvvmHelper = this;
        BindingContext = this._viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        this._viewModel.OnActivated();
    }

    public void OnListItemTapped(object sender, ItemTappedEventArgs e)
    {
        if (e.Item is TodoItem item)
        {
            Debug.WriteLine($"[UI] >>> Item clicked: {item.Id}");
            this._viewModel.SelectItemCommand.Execute(item);
        }

        if (sender is ListView itemList)
        {
            itemList.SelectedItem = null;
        }
    }

    public Task RunOnUiThreadAsync(Action func)
        => MainThread.InvokeOnMainThreadAsync(func);

    public Task DisplayErrorAlertAsync(string title, string message)
        => RunOnUiThreadAsync(async () => await DisplayAlert(title, message, "OK"));

}

