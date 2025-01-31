// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using TodoApp.MAUI.Models;
using TodoApp.MAUI.Services;

namespace TodoApp.MAUI.ViewModels;

public partial class MainViewModel(AppDbContext context, IAlertService alertService) : ObservableRecipient
{
    [ObservableProperty]
    private bool _isRefreshing = false;

    [ObservableProperty]
    private ConcurrentObservableCollection<TodoItem> items = [];

    [RelayCommand]
    public async Task RefreshItemsAsync(CancellationToken cancellationToken = default)
    {
        if (IsRefreshing)
        {
            return;
        }

        try
        {
            await context.SynchronizeAsync(cancellationToken);
            List<TodoItem> items = await context.TodoItems.ToListAsync(cancellationToken);
            Items.ReplaceAll(items);
        }
        catch (Exception ex)
        {
            await alertService.ShowErrorAlertAsync("RefreshItems", ex.Message);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    public async Task UpdateItemAsync(string itemId, CancellationToken cancellationToken = default)
    {
        try
        {
            TodoItem? item = await context.TodoItems.FindAsync([itemId]);
            if (item is not null)
            {
                item.IsComplete = !item.IsComplete;
                _ = context.TodoItems.Update(item);
                _ = Items.ReplaceIf(x => x.Id == itemId, item);
                _ = await context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await alertService.ShowErrorAlertAsync("UpdateItem", ex.Message);
        }
    }

    [RelayCommand]
    public async Task AddItemAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            TodoItem item = new() { Title = text };
            _ = context.TodoItems.Add(item);
            _ = await context.SaveChangesAsync(cancellationToken);
            Items.Add(item);
        }
        catch (Exception ex)
        {
            await alertService.ShowErrorAlertAsync("AddItem", ex.Message);
        }
    }
}
