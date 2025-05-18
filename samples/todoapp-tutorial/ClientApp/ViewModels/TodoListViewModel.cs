// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using ClientApp.Interfaces;
using ClientApp.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;

namespace ClientApp.ViewModels;

public partial class TodoListViewModel(ITodoService todoService, IAlertService alertService) : ObservableRecipient
{
    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private BindingList<TodoItem> items = [];

    [ObservableProperty]
    private string addItemTitle = string.Empty;

    [RelayCommand]
    public async Task AddItemAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var addition = await todoService.AddTodoItemAsync(AddItemTitle, cancellationToken);
            Items.Add(addition);
            AddItemTitle = string.Empty;
        }
        catch (Exception ex)
        {
            await alertService.ShowErrorAlertAsync("Error adding item", ex.Message);
        }
    }

    [RelayCommand]
    public async Task UpdateItemAsync(TodoItem item, CancellationToken cancellationToken = default)
    {
        try
        {
            TodoItem? storedItem = await todoService.GetTodoItemAsync(item.Id, cancellationToken);
            if (storedItem is not null)
            {
                storedItem.IsComplete = !storedItem.IsComplete;
                var replacedItem = await todoService.ReplaceTodoItemAsync(storedItem, cancellationToken);
                var idx = Items.IndexOf(item);
                Items[idx] = replacedItem;
            }
            else
            {
                await alertService.ShowErrorAlertAsync("Item not found", "The item was not found in the database.");
            }
        }
        catch (Exception ex)
        {
            await alertService.ShowErrorAlertAsync("Error updating item", ex.Message);
        }
    }

    [RelayCommand]
    public async Task RefreshItemsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsRefreshing = true;
            List<TodoItem> dbItems = await todoService.GetAllTodoItemsAsync(cancellationToken);
            Items.Clear();
            foreach (var dbItem in dbItems)
            {
                Items.Add(dbItem);
            }
        }
        catch (Exception ex)
        {
            await alertService.ShowErrorAlertAsync("Error refreshing items", ex.Message);
        }
        finally
        {
            IsRefreshing = false;
        }
    }
}