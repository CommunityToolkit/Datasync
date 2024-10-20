using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Datasync.Client;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using TodoApp.Avalonia.Database;
using TodoApp.Avalonia.Services;

namespace TodoApp.Avalonia.ViewModels;

public partial class TodoListViewModel(AppDbContext context) : ViewModelBase
{
    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private ConcurrentObservableCollection<TodoItemViewModel> items = [];
    
    // -- Adding new Items --
    
    /// <summary>
    /// This command is used to add a new Item to the List
    /// </summary>
    [RelayCommand (CanExecute = nameof(CanAddItem))]
    private async Task AddItemAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Create the new item
            TodoItem addition = new()
            {
                Id = Guid.NewGuid().ToString("N"),
                Title = NewItemContent ?? throw new InvalidOperationException("New item content may not be null or empty")
            };

            // Add the item to the database
            _ = context.TodoItems.Add(addition);
            _ = await context.SaveChangesAsync(cancellationToken);

            // Add the item to the end of the list
            Items.Add(new TodoItemViewModel(addition, this));

            // Update the title field ready for next insertion.
            NewItemContent = string.Empty;
        }
        catch (Exception ex)
        {
            this.ShowErrorAlert(ex.Message);
        }
    }

    /// <summary>
    /// Gets or set the content for new Items to add. If this string is not empty, the AddItemCommand will be enabled automatically
    /// </summary>
    [ObservableProperty] 
    [NotifyCanExecuteChangedFor(nameof(AddItemCommand))] // This attribute will invalidate the command each time this property changes
    private string? _newItemContent;

    /// <summary>
    /// Returns if a new Item can be added. We require to have the NewItem some Text
    /// </summary>
    private bool CanAddItem() => !string.IsNullOrWhiteSpace(NewItemContent);
    
    // -- Removing Items --
    
    /// <summary>
    /// Removes the given Item from the list
    /// </summary>
    /// <param name="item">the item to remove</param>
    [RelayCommand]
    private async Task RemoveItemAsync(TodoItemViewModel item)
    {
        // Remove the given item from the list
        Items.Remove(item);
        
        _ = context.TodoItems.Remove(item.GetToDoItem());
        _ = await context.SaveChangesAsync();
    }
    

    public async Task<bool> UpdateItemAsync(TodoItemViewModel item, CancellationToken cancellationToken = default)
    {
        try
        {
            TodoItem? storedItem = await context.TodoItems.FindAsync([item.GetToDoItem().Id], cancellationToken);
            if (storedItem is not null)
            {
                storedItem.IsComplete = item.IsChecked;

                // Store the updated item in the database
                _ = context.TodoItems.Update(storedItem);
                _ = await context.SaveChangesAsync(cancellationToken);
                
                item.IsChecked = storedItem.IsComplete;

                return true;
            }
            else
            {
                this.ShowErrorAlert("Item not found");
                return false;
            }
        }
        catch (Exception ex)
        {
            this.ShowErrorAlert(ex.Message);
            return false;
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
            _ = Items.AddRange(dbItems.Select(x => new TodoItemViewModel(x, this)));
        }
        catch (Exception ex)
        {
            this.ShowErrorAlert(ex.Message);
        }
    }
}