// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using TodoApp.MAUI.Models;
using TodoApp.MAUI.ViewModels;

namespace TodoApp.MAUI;

public partial class MainPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public MainPage()
    {
        InitializeComponent();
        this._viewModel = App.GetRequiredService<MainViewModel>();
        BindingContext = this._viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await this._viewModel.RefreshItemsAsync();
    }

    public async void OnListItemTapped(object sender, ItemTappedEventArgs e)
    {
        if (e.Item is TodoItem item)
        {
            await _viewModel.UpdateItemAsync(item.Id);
        }

        if (sender is ListView itemList)
        {
            itemList.SelectedItem = null;
        }
    }
}

