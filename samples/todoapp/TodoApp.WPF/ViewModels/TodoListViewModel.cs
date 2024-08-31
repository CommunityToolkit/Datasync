// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using TodoApp.WPF.Database;
using TodoApp.WPF.Services;

namespace TodoApp.WPF.ViewModels;

public partial class TodoListViewModel(AppDbContext context, IAlertService alertService) : ObservableRecipient
{
    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private ConcurrentObservableCollection<TodoItem> items = [];

    [ObservableProperty]
    private string addItemTitle = string.Empty;

    [RelayCommand]
    public async Task AddItemAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Create the new item
            TodoItem addition = new()
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = AddItemTitle
            };

            // Add the item to the database
            _ = context.TodoItems.Add(addition);
            _ = await context.SaveChangesAsync(cancellationToken);

            // Add the item to the end of the list
            Items.Add(addition);

            // Update the title field ready for next insertion.
            AddItemTitle = string.Empty;
        }
        catch (Exception ex)
        {
            await alertService.ShowErrorAlertAsync("AddItem", ex.Message);
        }
    }

    [RelayCommand]
    public async Task UpdateItemAsync(TodoItem item, CancellationToken cancellationToken = default)
    {
        try
        {
            TodoItem? storedItem = await context.TodoItems.FindAsync([item.Id], cancellationToken);
            if (storedItem is not null)
            {
                storedItem.IsComplete = !storedItem.IsComplete;

                // Store the updated item in the database
                _ = context.TodoItems.Update(storedItem);
                _ = await context.SaveChangesAsync(cancellationToken);

                // WPF does not respect changes to the observable collection in other threads
                // so we just refresh the items
                List<TodoItem> dbItems = await context.TodoItems.ToListAsync(cancellationToken);
                Items.Clear();
                _ = Items.AddRange(dbItems);
            }
            else
            {
                await alertService.ShowErrorAlertAsync("UpdateItem", "Item not found");
            }
        }
        catch (Exception ex)
        {
            await alertService.ShowErrorAlertAsync("UpdateItem", ex.Message);
        }
    }

    [RelayCommand]
    public async Task RefreshItemsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Synchronize with the remote service
            await context.SynchronizeAsync(cancellationToken);

            // Retrieve the items from the service
            List<TodoItem> dbItems = await context.TodoItems.ToListAsync(cancellationToken);

            // Replace the items in the collection
            Items.Clear();
            _ = Items.AddRange(dbItems);
        }
        catch (Exception ex)
        {
            await alertService.ShowErrorAlertAsync("AddItem", ex.Message);
        }
    }
}
