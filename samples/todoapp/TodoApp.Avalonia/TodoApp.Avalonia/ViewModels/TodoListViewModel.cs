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

/// <summary>
/// A ViewModel used to represent a list of <see cref="TodoItem"/>s
/// </summary>
/// <param name="context">the <see cref="AppDbContext"/> to use</param>
public partial class TodoListViewModel(AppDbContext context) : ViewModelBase, IDialogBus
{
    /// <summary>
    /// Gets or sets if the data is currently being refreshed.
    /// </summary>
    [ObservableProperty]
    private bool isRefreshing;

    /// <summary>
    /// Gets or sets a collection of <see cref="TodoItemViewModel"/>s
    /// </summary>
    [ObservableProperty]
    private ConcurrentObservableCollection<TodoItemViewModel> items = [];
    
    // -- Adding new Items --
    
    /// <summary>
    /// This command is used to add a new Item to the <see cref="Items"/>
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
            Items.Add(new TodoItemViewModel(addition, this, context));

            // Clear the NewItemContent-property for next insertion.
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
    /// Returns <c>true</c> if a new Item can be added. We require to have the NewItem some text.
    /// </summary>
    private bool CanAddItem() => !string.IsNullOrWhiteSpace(NewItemContent);
    
    // -- Removing Items --
    
    /// <summary>
    /// This command removes the given Item from the <see cref="Items"/>-list
    /// </summary>
    /// <param name="item">the item to remove</param>
    [RelayCommand]
    private async Task RemoveItemAsync(TodoItemViewModel item)
    {
        // Remove the item from the database
        _ = context.TodoItems.Remove(item.GetToDoItem());
        _ = await context.SaveChangesAsync();
        
        // Remove the given item from the list
        Items.Remove(item);
    }
    

    /// <summary>
    /// This command is used to refresh the entire <see cref="Items"/>-list.
    /// </summary>
    [RelayCommand]
    public async Task RefreshItemsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Synchronize with the remote service
            await context.SynchronizeAsync(cancellationToken);

            // Retrieve the items from the service
            List<TodoItem> dbItems = await context.TodoItems.OrderBy(item => item.Id).ToListAsync(cancellationToken);

            // Replace the items in the collection
            Items.Clear();
            _ = Items.AddRange(dbItems.Select(x => new TodoItemViewModel(x, this, context)));
            
            this.ShowSuccessAlert("All Items loaded");
        }
        catch (Exception ex)
        {
            this.ShowErrorAlert(ex.Message);
        }
    }
}