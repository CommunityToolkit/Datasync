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
        this._viewModel = ((App)App.Current!).Services.GetRequiredService<MainViewModel>();
        BindingContext = this._viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        this._viewModel.RefreshItemsCommand.Execute(null);
    }

    public void OnListItemTapped(object sender, ItemTappedEventArgs e)
    {
        if (e.Item is TodoItem item)
        {
            this._viewModel.UpdateItemCommand.Execute(item.Id);
        }

        if (sender is ListView itemList)
        {
            itemList.SelectedItem = null;
        }
    }

    public void OnAddItemEntryCompleted(object sender, EventArgs e)
    {
        if (sender is not null and Entry entry)
        {
            this._viewModel.AddItemCommand.Execute(entry.Text);
            entry.Text = string.Empty;
        }
    }
}

