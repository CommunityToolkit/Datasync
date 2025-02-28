// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Datasync.Client;
using Microsoft.EntityFrameworkCore;
using TodoApp.Uno.Database;

#nullable disable

namespace TodoApp.Uno.ViewModels;

/// <summary>
/// The view model for the TodoListWindow.
/// </summary>
public partial class TodoListViewModel(AppDbContext service) : ObservableRecipient
{
    internal event EventHandler<NotificationEventArgs> NotificationHandler;

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private ConcurrentObservableCollection<TodoItemViewModel> items = [];

    [ObservableProperty]
    private string title = string.Empty;

    [RelayCommand]
    public async Task AddItemAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a new item
            TodoItem addition = new()
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = Title,
            };

            // Add te item to the database
            _ = service.TodoItems.Add(addition);
            _ = await service.SaveChangesAsync(cancellationToken);

            // Add the item to the end of the list.
            Items.Add(new TodoItemViewModel(addition));

            // Update the title field ready for ext insertion.
            Title = string.Empty;
        }
        catch (Exception ex)
        {
            NotificationHandler?.Invoke(this, new NotificationEventArgs(ex.GetType().Name, ex.Message, true));
        }
        finally
        {
            NotificationHandler?.Invoke(this, new NotificationEventArgs("Item Added", "", false));
        }
    }

    [RelayCommand]
    public async Task EditItemAsync(string itemId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Retrieve the item (by ID) from the service.
            TodoItem item = await service.TodoItems.FindAsync([itemId], cancellationToken)
                ?? throw new ApplicationException($"Item with ID '{itemId}' not found.");

            // Update the item in the database
            item.IsComplete = !item.IsComplete;
            _ = service.TodoItems.Update(item);
            _ = await service.SaveChangesAsync(cancellationToken);

            // Update the item in the list
            _ = Items.ReplaceIf(x => x.Id == itemId, new TodoItemViewModel(item));
        }
        catch (Exception ex)
        {
            NotificationHandler?.Invoke(this, new NotificationEventArgs(ex.GetType().Name, ex.Message, true));
        }
        finally
        {
            NotificationHandler?.Invoke(this, new NotificationEventArgs("Item Updated", "", false));
        }
    }

    [RelayCommand]
    public async Task LoadPageAsync(CancellationToken cancellationToken = default)
    {
        await RefreshItemsAsync(cancellationToken);
    }

    [RelayCommand]
    public async Task RefreshItemsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IsRefreshing = true;

            // Synchronize data with the remote service (if any).
            await service.SynchronizeAsync(cancellationToken);

            // Pull all items from the database.
            IEnumerable<TodoItem> itemsFromDatabase = await service.TodoItems.ToListAsync(cancellationToken);

            IEnumerable<TodoItemViewModel> items = itemsFromDatabase.OrderBy(item => item.Id).Select(x => new TodoItemViewModel(x));

            // Replace all the items in the collection.
            Items.ReplaceAll(items);
        }
        catch (Exception ex)
        {
            NotificationHandler?.Invoke(this, new NotificationEventArgs(ex.GetType().Name, ex.Message, true));
        }
        finally
        {
            IsRefreshing = false;
            NotificationHandler?.Invoke(this, new NotificationEventArgs("Items Refreshed", "", false));
        }
    }
}
