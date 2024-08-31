// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
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
    public async Task AddItemCommand(CancellationToken cancellationToken = default)
    {
        Debug.WriteLine("AddItemCommand");
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
        finally
        {
            await alertService.ShowInfoAlertAsync("Item Added");
        }
    }

    [RelayCommand]
    public async Task RefreshItemsCommand(CancellationToken cancellationToken = default)
    {
        Debug.WriteLine("RefreshItemsCommand");
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
        finally
        {
            await alertService.ShowInfoAlertAsync("Items Refreshed");
        }
    }
}
